// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.Secrets;

/// <summary>
/// Creates the secrets provider an environment names.
/// </summary>
public static class SecretsProviders
{
    /// <summary>
    /// Creates the provider of an environment document.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <param name="document">The environment document.</param>
    /// <param name="passphrase">The passphrase source, used by the passphrase provider.</param>
    /// <param name="keyVault">Optional Key Vault access, defaulting to <see cref="AzureKeyVaultSecrets"/>.</param>
    /// <param name="vaultHandler">Optional HTTP handler for HashiCorp Vault, for tests.</param>
    /// <returns>The provider.</returns>
    public static ISecretsProvider Create(
        string environment,
        EnvironmentDocument document,
        IPassphraseSource passphrase,
        IKeyVaultSecrets? keyVault = null,
        HttpMessageHandler? vaultHandler = null) =>
        Create(environment, SecretsProviderSpec.Parse(document.SecretsProvider), document, passphrase, keyVault, vaultHandler);

    /// <summary>
    /// Creates the provider for a specification.
    /// </summary>
    /// <param name="environment">The environment name.</param>
    /// <param name="spec">The provider specification.</param>
    /// <param name="document">The environment document, which carries the passphrase provider's salt.</param>
    /// <param name="passphrase">The passphrase source, used by the passphrase provider.</param>
    /// <param name="keyVault">Optional Key Vault access, defaulting to <see cref="AzureKeyVaultSecrets"/>.</param>
    /// <param name="vaultHandler">Optional HTTP handler for HashiCorp Vault, for tests.</param>
    /// <returns>The provider.</returns>
    /// <exception cref="EnvironmentException">Thrown when the provider kind is not supported.</exception>
    public static ISecretsProvider Create(
        string environment,
        SecretsProviderSpec spec,
        EnvironmentDocument document,
        IPassphraseSource passphrase,
        IKeyVaultSecrets? keyVault = null,
        HttpMessageHandler? vaultHandler = null) =>
        spec.Kind switch
        {
            SecretsProviderKind.Passphrase => new PassphraseSecretsProvider(document, passphrase),
            SecretsProviderKind.AzureKeyVault => new AzureKeyVaultSecretsProvider(environment, keyVault ?? new AzureKeyVaultSecrets(spec.Target)),
            SecretsProviderKind.HashiCorpVault => new HashiCorpVaultSecretsProvider(spec, environment, vaultHandler),
            _ => throw new EnvironmentException($"Unsupported secrets provider '{spec.Text}'.")
        };
}
