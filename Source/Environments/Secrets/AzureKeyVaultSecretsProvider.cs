// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Environments.Secrets;

/// <summary>
/// Keeps secrets in Azure Key Vault; the environment file holds the secret name as a reference.
/// </summary>
/// <param name="environment">The environment name, used to keep environments apart in one vault.</param>
/// <param name="secrets">The vault access.</param>
public class AzureKeyVaultSecretsProvider(string environment, IKeyVaultSecrets secrets) : ISecretsProvider
{
    /// <summary>
    /// Builds the Key Vault secret name for a key: letters, digits and dashes only, with a hash suffix that keeps different keys apart.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <param name="key">The configuration key.</param>
    /// <returns>The secret name.</returns>
    public static string NameFor(string environment, string key)
    {
        var readable = string.Concat($"cratis-{environment}-{key}".Select(c => char.IsAsciiLetterOrDigit(c) ? c : '-')).Trim('-');
        if (readable.Length > 100)
        {
            readable = readable[..100].TrimEnd('-');
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"{environment}\n{key}")))[..8];
        return $"{readable}-{hash}";
    }

    /// <inheritdoc/>
    public async Task<ConfigEntry> StoreAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var name = NameFor(environment, key);
        try
        {
            await secrets.SetAsync(name, value, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new SecretNotAvailableException($"Could not store '{key}' in Azure Key Vault: {ex.GetType().Name}.", ex);
        }

        return new ConfigEntry(ConfigEntryKind.Reference, name);
    }

    /// <inheritdoc/>
    public async Task<string> ResolveAsync(string key, ConfigEntry entry, CancellationToken cancellationToken = default)
    {
        string? value;
        try
        {
            value = await secrets.GetAsync(entry.Value, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new SecretNotAvailableException($"Could not read '{key}' from Azure Key Vault: {ex.GetType().Name}.", ex);
        }

        return value ?? throw new SecretNotAvailableException($"The secret for '{key}' ({entry.Value}) does not exist in the vault.");
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key, ConfigEntry entry, CancellationToken cancellationToken = default) =>
        entry.Kind == ConfigEntryKind.Reference ? secrets.DeleteAsync(entry.Value, cancellationToken) : Task.CompletedTask;
}
