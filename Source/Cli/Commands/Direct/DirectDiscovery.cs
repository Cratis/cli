// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.Commands.Direct;

/// <summary>Resolves the authorization server from RFC 9728 and its endpoints from RFC 8414 or OIDC metadata.</summary>
/// <param name="http">HTTP transport.</param>
internal sealed class DirectDiscovery(HttpClient http)
{
    /// <summary>Validates an issuer: HTTPS, or HTTP on a loopback host for local development, without query, fragment, or credentials.</summary>
    /// <param name="value">The issuer.</param>
    /// <returns>The validated issuer.</returns>
    /// <exception cref="DirectAuthError">When the issuer is not allowed.</exception>
    internal static Uri ValidateIssuer(string? value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var issuer) || !DirectIssuerScheme.IsAllowed(issuer) ||
            issuer.UserInfo.Length != 0 || issuer.Query.Length != 0 || issuer.Fragment.Length != 0)
        {
            throw new DirectAuthError("The authorization server issuer must be an HTTPS URL (plain HTTP only on localhost or a loopback address) without query, fragment, or credentials.");
        }

        return issuer;
    }

    internal async Task<DirectEndpoints> Discover(DirectTarget target, string? explicitIssuer, CancellationToken cancellationToken)
    {
        var resourceMetadata = new Uri(target.Origin, "/.well-known/oauth-protected-resource/mcp");
        using var resource = await Get(resourceMetadata, allowMissing: true, cancellationToken);
        Uri issuer;
        if (resource is not null)
        {
            var body = resource.RootElement;
            if (RequiredString(body, "resource") != target.Resource.AbsoluteUri ||
                !body.TryGetProperty("authorization_servers", out var servers) || servers.ValueKind != JsonValueKind.Array ||
                servers.GetArrayLength() == 0)
            {
                throw new DirectAuthError("Direct returned invalid protected-resource metadata.");
            }

            if (servers.EnumerateArray().Any(server => server.ValueKind != JsonValueKind.String))
            {
                throw new DirectAuthError("Direct returned invalid authorization server metadata.");
            }

            var offered = servers.EnumerateArray().Select(server => ValidateIssuer(server.GetString())).ToArray();
            issuer = explicitIssuer is null ? offered[0] : ValidateIssuer(explicitIssuer);
            if (!offered.Any(candidate => candidate.OriginalString == issuer.OriginalString))
            {
                throw new DirectAuthError("The requested issuer is not listed by the Direct resource.");
            }
        }
        else
        {
            if (explicitIssuer is null)
            {
                throw new DirectAuthError("Direct did not publish protected-resource metadata. Supply --issuer to login.");
            }

            issuer = ValidateIssuer(explicitIssuer);
        }

        return await DiscoverIssuer(issuer, cancellationToken);
    }

    internal async Task<DirectEndpoints> DiscoverIssuer(Uri issuer, CancellationToken cancellationToken)
    {
        issuer = ValidateIssuer(issuer.OriginalString);
        var suffix = issuer.AbsolutePath.TrimEnd('/');
        var authority = issuer.GetLeftPart(UriPartial.Authority);
        using var document = await Get(new Uri($"{authority}/.well-known/oauth-authorization-server{suffix}"), allowMissing: true, cancellationToken)
            ?? await Get(new Uri($"{authority}{suffix}/.well-known/openid-configuration"), allowMissing: false, cancellationToken)
            ?? throw new DirectAuthError("Authorization server discovery failed.");
        var metadata = document.RootElement;
        if (RequiredString(metadata, "issuer") != issuer.OriginalString)
        {
            throw new DirectAuthError("Authorization server issuer mismatch.");
        }

        return new(issuer, Endpoint(metadata, "authorization_endpoint", issuer), Endpoint(metadata, "token_endpoint", issuer), Endpoint(metadata, "revocation_endpoint", issuer));
    }

    static Uri Endpoint(JsonElement metadata, string name, Uri issuer)
    {
        var value = RequiredString(metadata, name);
        if (!Uri.TryCreate(value, UriKind.Absolute, out var endpoint) || !DirectIssuerScheme.IsAllowedEndpoint(issuer, endpoint) || endpoint.UserInfo.Length != 0 || endpoint.Fragment.Length != 0)
        {
            throw new DirectAuthError($"Authorization server has an invalid {name}.");
        }

        return endpoint;
    }

    static string RequiredString(JsonElement metadata, string name)
    {
        if (!metadata.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new DirectAuthError($"Discovery metadata is missing {name}.");
        }

        return property.GetString()!;
    }

    async Task<JsonDocument?> Get(Uri uri, bool allowMissing, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (allowMissing && response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new DirectAuthError($"Discovery returned HTTP {(int)response.StatusCode}.");
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var bytes = new byte[65537];
        var length = 0;
        while (length < bytes.Length)
        {
            var count = await stream.ReadAsync(bytes.AsMemory(length), cancellationToken);
            if (count == 0)
            {
                break;
            }

            length += count;
        }

        if (length > 65536)
        {
            throw new DirectAuthError("Discovery response is too large.");
        }

        try
        {
            return JsonDocument.Parse(bytes.AsMemory(0, length));
        }
        catch (JsonException)
        {
            throw new DirectAuthError("Discovery returned invalid JSON.");
        }
    }
}
