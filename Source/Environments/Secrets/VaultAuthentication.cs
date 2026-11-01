// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http.Json;
using System.Text.Json;

namespace Cratis.Environments.Secrets;

/// <summary>
/// Obtains a HashiCorp Vault client token with a token, Kubernetes or AppRole login, chosen from the standard environment variables:
/// <c language="csharp">VAULT_TOKEN</c>; <c language="csharp">VAULT_ROLE_ID</c> and <c language="csharp">VAULT_SECRET_ID</c>; or <c language="csharp">VAULT_K8S_ROLE</c> (service account token from <c language="csharp">VAULT_K8S_TOKEN_FILE</c>).
/// <c language="csharp">VAULT_AUTH_MOUNT</c> overrides the auth method mount.
/// </summary>
public static class VaultAuthentication
{
    const string DefaultServiceAccountToken = "/var/run/secrets/kubernetes.io/serviceaccount/token";

    /// <summary>
    /// Resolves a client token.
    /// </summary>
    /// <param name="http">The HTTP client, with its base address set to the Vault address.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The client token.</returns>
    /// <exception cref="EnvironmentException">Thrown when no authentication is configured or the login fails.</exception>
    public static async Task<string> GetTokenAsync(HttpClient http, CancellationToken cancellationToken)
    {
        var token = Environment.GetEnvironmentVariable("VAULT_TOKEN");
        if (!string.IsNullOrEmpty(token))
        {
            return token;
        }

        var mount = Environment.GetEnvironmentVariable("VAULT_AUTH_MOUNT");
        var roleId = Environment.GetEnvironmentVariable("VAULT_ROLE_ID");
        if (!string.IsNullOrEmpty(roleId))
        {
            var secretId = Environment.GetEnvironmentVariable("VAULT_SECRET_ID") ?? string.Empty;
            return await LoginAsync(http, $"v1/auth/{mount ?? "approle"}/login", new { role_id = roleId, secret_id = secretId }, cancellationToken);
        }

        var role = Environment.GetEnvironmentVariable("VAULT_K8S_ROLE");
        if (!string.IsNullOrEmpty(role))
        {
            var tokenFile = Environment.GetEnvironmentVariable("VAULT_K8S_TOKEN_FILE") ?? DefaultServiceAccountToken;
            if (!File.Exists(tokenFile))
            {
                throw new EnvironmentException($"Kubernetes service account token file not found: {tokenFile}");
            }

            var jwt = (await File.ReadAllTextAsync(tokenFile, cancellationToken)).Trim();
            return await LoginAsync(http, $"v1/auth/{mount ?? "kubernetes"}/login", new { role, jwt }, cancellationToken);
        }

        throw new EnvironmentException("No HashiCorp Vault authentication configured. Set VAULT_TOKEN, or VAULT_ROLE_ID and VAULT_SECRET_ID (AppRole), or VAULT_K8S_ROLE (Kubernetes).");
    }

    static async Task<string> LoginAsync(HttpClient http, string path, object body, CancellationToken cancellationToken)
    {
        using var response = await http.PostAsJsonAsync(path, body, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new EnvironmentException($"HashiCorp Vault login failed with HTTP {(int)response.StatusCode}.");
        }

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return json.RootElement.GetProperty("auth").GetProperty("client_token").GetString()
            ?? throw new EnvironmentException("HashiCorp Vault login returned no client token.");
    }
}
