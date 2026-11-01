// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace Cratis.Environments.Secrets;

/// <summary>
/// Azure Key Vault access through <see cref="DefaultAzureCredential"/> (CLI login, managed identity, workload identity).
/// </summary>
/// <param name="vault">A vault name (<c language="csharp">my-vault</c>) or a vault URL.</param>
public class AzureKeyVaultSecrets(string vault) : IKeyVaultSecrets
{
    readonly SecretClient _client = new(ToUri(vault), new DefaultAzureCredential());

    /// <summary>
    /// Resolves a vault name or URL to the vault URI.
    /// </summary>
    /// <param name="vault">A vault name or URL.</param>
    /// <returns>The URI.</returns>
    public static Uri ToUri(string vault)
    {
        if (vault.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return new Uri(vault);
        }

        return vault.Contains('.', StringComparison.Ordinal) ? new Uri($"https://{vault}") : new Uri($"https://{vault}.vault.azure.net");
    }

    /// <inheritdoc/>
    public async Task SetAsync(string name, string value, CancellationToken cancellationToken) =>
        await _client.SetSecretAsync(name, value, cancellationToken);

    /// <inheritdoc/>
    public async Task<string?> GetAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.GetSecretAsync(name, cancellationToken: cancellationToken);
            return response.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            await _client.StartDeleteSecretAsync(name, cancellationToken);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
        }
    }
}
