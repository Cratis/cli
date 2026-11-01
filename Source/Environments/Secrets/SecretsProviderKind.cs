// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.Secrets;

/// <summary>
/// The kinds of secrets provider.
/// </summary>
public enum SecretsProviderKind
{
    /// <summary>
    /// Inline AES-256-GCM encryption with a passphrase-derived key.
    /// </summary>
    Passphrase,

    /// <summary>
    /// Azure Key Vault.
    /// </summary>
    AzureKeyVault,

    /// <summary>
    /// HashiCorp Vault KV v2.
    /// </summary>
    HashiCorpVault
}
