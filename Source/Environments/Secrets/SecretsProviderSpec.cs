// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments.Secrets;

/// <summary>
/// A parsed secrets provider specification such as <c language="csharp">passphrase</c>, <c language="csharp">azurekeyvault://my-vault</c> or <c language="csharp">hashivault://vault.example.com:8200/secret/cratis</c>.
/// </summary>
/// <param name="Kind">The provider kind.</param>
/// <param name="Target">The part after the scheme, empty for the passphrase provider.</param>
/// <param name="Text">The normalized specification stored in the environment file.</param>
public record SecretsProviderSpec(SecretsProviderKind Kind, string Target, string Text)
{
    /// <summary>
    /// Parses a provider specification. <c language="csharp">azure-keyvault://</c> is accepted as an alias of <c language="csharp">azurekeyvault://</c>;
    /// <c language="csharp">hashivault+http://</c> selects plain HTTP for development servers.
    /// </summary>
    /// <param name="text">The specification.</param>
    /// <returns>The parsed specification.</returns>
    /// <exception cref="EnvironmentException">Thrown when the specification is not recognized.</exception>
    public static SecretsProviderSpec Parse(string? text)
    {
        text = text?.Trim();
        if (string.IsNullOrEmpty(text) || string.Equals(text, EnvironmentDocument.PassphraseProvider, StringComparison.OrdinalIgnoreCase))
        {
            return new(SecretsProviderKind.Passphrase, string.Empty, EnvironmentDocument.PassphraseProvider);
        }

        foreach (var prefix in new[] { "azurekeyvault://", "azure-keyvault://" })
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && text.Length > prefix.Length)
            {
                var vault = text[prefix.Length..].TrimEnd('/');
                return new(SecretsProviderKind.AzureKeyVault, vault, $"azurekeyvault://{vault}");
            }
        }

        foreach (var prefix in new[] { "hashivault://", "hashivault+http://" })
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var target = text[prefix.Length..].Trim('/');
                if (!target.Contains('/', StringComparison.Ordinal))
                {
                    throw new EnvironmentException("A HashiCorp Vault provider needs an address and a KV v2 mount: hashivault://<address>/<mount>[/<path>]");
                }

                return new(SecretsProviderKind.HashiCorpVault, target, prefix.ToLowerInvariant() + target);
            }
        }

        throw new EnvironmentException($"Unknown secrets provider '{text}'. Use passphrase, azurekeyvault://<vault> or hashivault://<address>/<mount>[/<path>].");
    }
}
