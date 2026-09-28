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

/// <summary>A safe error at a Direct authentication boundary.</summary>
/// <param name="message">Message with no credentials.</param>
internal sealed class DirectAuthError(string message) : Exception(message);

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

        var errors = parameters.Where(entry => entry.Key == "error").ToArray();
        if (errors.Length != 0)
        {
            throw new DirectAuthError("Authorization server declined the login.");
        }

        var code = Unique("code");
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DirectAuthError("Authorization response did not contain a code.");
        }

        return code;
    }
}
