// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Cli.Commands.Direct;

/// <summary>The validated endpoints of an authorization server.</summary>
/// <param name="Issuer">Discovered issuer.</param>
/// <param name="Authorization">Authorization endpoint.</param>
/// <param name="Token">Token endpoint.</param>
/// <param name="Revocation">Revocation endpoint.</param>
internal sealed record DirectEndpoints(Uri Issuer, Uri Authorization, Uri Token, Uri Revocation);

/// <summary>The canonical Direct origin, resource, and tenant credential identity.</summary>
/// <param name="Origin">Direct origin.</param>
/// <param name="Tenant">Tenant hint.</param>
internal sealed record DirectTarget(Uri Origin, string? Tenant)
{
    internal Uri Resource => new(Origin, "/mcp");

    internal string Key => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{Origin.AbsoluteUri}|{Resource.AbsoluteUri}|{Tenant ?? string.Empty}")));

    internal static DirectTarget Create(string origin, string? tenant)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.AbsolutePath != "/")
        {
            throw new DirectAuthError("Direct --url must be an HTTPS origin without a path, query, or credentials.");
        }

        if (tenant is not null && (string.IsNullOrWhiteSpace(tenant) || tenant.Length > 256 || tenant.Any(char.IsControl)))
        {
            throw new DirectAuthError("Invalid tenant hint.");
        }

        return new DirectTarget(new Uri(uri.GetLeftPart(UriPartial.Authority) + "/"), tenant);
    }
}

/// <summary>Transport rules for authorization server addresses.</summary>
internal static class DirectIssuerScheme
{
    /// <summary>Checks that an issuer is HTTPS, or plain HTTP on a loopback host (localhost or a loopback IP) for local development.</summary>
    /// <param name="issuer">The issuer.</param>
    /// <returns>True when tokens may be sent to it.</returns>
    internal static bool IsAllowed(Uri issuer) =>
        issuer.IsAbsoluteUri && (issuer.Scheme == Uri.UriSchemeHttps || IsLoopbackHttp(issuer));

    /// <summary>Checks that an endpoint published by an issuer may be used: HTTPS, or loopback HTTP only for a loopback HTTP issuer.</summary>
    /// <param name="issuer">The validated issuer.</param>
    /// <param name="endpoint">The published endpoint.</param>
    /// <returns>True when tokens may be sent to it.</returns>
    internal static bool IsAllowedEndpoint(Uri issuer, Uri endpoint) =>
        endpoint.IsAbsoluteUri && (endpoint.Scheme == Uri.UriSchemeHttps || (IsLoopbackHttp(issuer) && IsLoopbackHttp(endpoint)));

    static bool IsLoopbackHttp(Uri uri) => uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
}

/// <summary>A safe error at a Direct authentication boundary.</summary>
/// <param name="message">Message with no credentials.</param>
internal sealed class DirectAuthError(string message) : Exception(message);

/// <summary>A state- and issuer-validated OAuth error response that ends the login attempt.</summary>
internal sealed class DirectAuthorizationDeclined : Exception
{
    static readonly string[] _known =
    [
        "access_denied", "invalid_scope", "invalid_request", "server_error", "temporarily_unavailable",
        "unauthorized_client", "consent_required", "login_required"
    ];

    /// <summary>Initializes a new instance of the <see cref="DirectAuthorizationDeclined"/> class.</summary>
    /// <param name="error">The raw error parameter; only allow-listed values are retained.</param>
    internal DirectAuthorizationDeclined(string error)
        : base("Authorization server declined the login.")
    {
        Error = _known.Contains(error, StringComparer.Ordinal) ? error : "unknown";
    }

    /// <summary>Gets the allow-listed OAuth error code, or "unknown".</summary>
    internal string Error { get; }

    /// <summary>Gets a user-facing message with the restricted error code and what to do next.</summary>
    internal string Guidance => $"Authorization server declined the Direct login ({Error}). " + Error switch
    {
        "access_denied" => "Sign-in or consent was declined, or your account may not use the requested tenant. Run the login again and choose a tenant you belong to.",
        "invalid_scope" => "The requested Direct scopes were refused. Check that --url points to Direct and that its authorization server offers the Direct scopes.",
        "invalid_request" or "unauthorized_client" => "The authorization server refused this CLI's request. Check --url and --issuer, or update the Cratis CLI.",
        "server_error" or "temporarily_unavailable" => "The authorization server could not complete the request. Try again later.",
        "consent_required" or "login_required" => "Sign-in and consent must be completed in the browser. Run the login again.",
        _ => "The authorization server returned an unrecognized error. Run the login again, and check --url and --issuer if it persists."
    };
}

/// <summary>PKCE and state values for a single browser transaction.</summary>
/// <param name="Verifier">Private PKCE verifier.</param>
/// <param name="Challenge">Public S256 challenge.</param>
/// <param name="State">Random transaction state.</param>
internal sealed record DirectChallenge(string Verifier, string Challenge, string State)
{
    internal static DirectChallenge Create()
    {
        var verifier = Encode(RandomNumberGenerator.GetBytes(32));
        return new(verifier, Encode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))), Encode(RandomNumberGenerator.GetBytes(32)));
    }

    static string Encode(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

/// <summary>Checks the exact loopback callback, transaction state and issuing server.</summary>
internal static class DirectCallback
{
    internal static string Validate(Uri expectedRedirect, Uri actual, string state, Uri issuer)
    {
        if (actual.GetLeftPart(UriPartial.Path) != expectedRedirect.AbsoluteUri || actual.Fragment.Length != 0)
        {
            throw new DirectAuthError("Unexpected OAuth redirect address.");
        }

        var parameters = actual.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Select(part => new KeyValuePair<string, string>(Uri.UnescapeDataString(part[0].Replace('+', ' ')), part.Length == 2 ? Uri.UnescapeDataString(part[1].Replace('+', ' ')) : string.Empty))
            .ToArray();
        string? Unique(string key)
        {
            var matching = parameters.Where(entry => entry.Key == key).ToArray();
            if (matching.Length != 1)
            {
                throw new DirectAuthError($"Missing or repeated OAuth {key} parameter.");
            }

            return matching[0].Value;
        }

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(Unique("state")!), Encoding.UTF8.GetBytes(state)))
        {
            throw new DirectAuthError("OAuth state did not match this login attempt.");
        }

        if (Unique("iss") != issuer.OriginalString)
        {
            throw new DirectAuthError("OAuth issuer did not match discovery metadata.");
        }

        // State and issuer prove this response belongs to this login attempt, so an error is final.
        var errors = parameters.Where(entry => entry.Key == "error").ToArray();
        if (errors.Length != 0)
        {
            throw new DirectAuthorizationDeclined(errors.Length == 1 ? errors[0].Value : string.Empty);
        }

        var code = Unique("code");
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DirectAuthError("Authorization response did not contain a code.");
        }

        return code;
    }
}
