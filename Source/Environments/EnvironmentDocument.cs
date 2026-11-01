// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Environments;

/// <summary>
/// The contents of one <c language="csharp">.cratis/environments/&lt;name&gt;.yaml</c> file.
/// </summary>
public class EnvironmentDocument
{
    /// <summary>
    /// The secrets provider name for the passphrase provider.
    /// </summary>
    public const string PassphraseProvider = "passphrase";

    /// <summary>
    /// Gets or sets the secrets provider: <c language="csharp">passphrase</c>, <c language="csharp">azurekeyvault://&lt;vault&gt;</c> or <c language="csharp">hashivault://&lt;address&gt;/&lt;mount&gt;[/&lt;path&gt;]</c>.
    /// </summary>
    public string SecretsProvider { get; set; } = PassphraseProvider;

    /// <summary>
    /// Gets or sets the random per-environment salt (base64) used by the passphrase provider.
    /// </summary>
    public string? EncryptionSalt { get; set; }

    /// <summary>
    /// Gets or sets the key derivation parameters used by the passphrase provider.
    /// </summary>
    public KdfParameters? Kdf { get; set; }

    /// <summary>
    /// Gets or sets the verification value (base64) that lets a wrong passphrase fail cleanly.
    /// </summary>
    public string? EncryptionCheck { get; set; }

    /// <summary>
    /// Gets the configuration entries by key.
    /// </summary>
    public SortedDictionary<string, ConfigEntry> Config { get; } = new(StringComparer.Ordinal);
}
