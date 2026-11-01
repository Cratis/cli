// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Environments.Secrets;

namespace Cratis.Environments;

/// <summary>
/// An opened environment: its values, with secrets resolved through the environment's secrets provider.
/// </summary>
public class ProjectEnvironment
{
    readonly ProjectEnvironments _environments;
    readonly IPassphraseSource _passphrase;
    readonly IKeyVaultSecrets? _keyVault;
    readonly HttpMessageHandler? _vaultHandler;
    ISecretsProvider? _provider;

    ProjectEnvironment(ProjectEnvironments environments, string name, EnvironmentDocument document, IPassphraseSource passphrase, IKeyVaultSecrets? keyVault, HttpMessageHandler? vaultHandler)
    {
        _environments = environments;
        Name = name;
        Document = document;
        _passphrase = passphrase;
        _keyVault = keyVault;
        _vaultHandler = vaultHandler;
    }

    /// <summary>
    /// Gets the environment name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the underlying document.
    /// </summary>
    public EnvironmentDocument Document { get; private set; }

    /// <summary>
    /// Gets the configuration keys, sorted.
    /// </summary>
    public IReadOnlyList<string> Keys => [.. Document.Config.Keys];

    /// <summary>
    /// Gets the secrets provider specification.
    /// </summary>
    public SecretsProviderSpec Provider => SecretsProviderSpec.Parse(Document.SecretsProvider);

    /// <summary>
    /// Creates a new environment file with the given secrets provider.
    /// </summary>
    /// <param name="environments">The project environments.</param>
    /// <param name="name">The environment name.</param>
    /// <param name="providerSpec">The secrets provider specification, defaulting to the passphrase provider.</param>
    /// <param name="passphrase">The passphrase source; asked for a new passphrase only by the passphrase provider.</param>
    /// <param name="kdf">Optional key derivation parameters.</param>
    /// <param name="keyVault">Optional Key Vault access.</param>
    /// <param name="vaultHandler">Optional HTTP handler for HashiCorp Vault.</param>
    /// <returns>The new, saved environment.</returns>
    /// <exception cref="EnvironmentException">Thrown when the environment already exists.</exception>
    public static ProjectEnvironment Create(
        ProjectEnvironments environments,
        string name,
        string? providerSpec,
        IPassphraseSource passphrase,
        KdfParameters? kdf = null,
        IKeyVaultSecrets? keyVault = null,
        HttpMessageHandler? vaultHandler = null)
    {
        if (environments.Exists(name))
        {
            throw new EnvironmentException($"Environment '{name}' already exists.");
        }

        var spec = SecretsProviderSpec.Parse(providerSpec);
        var document = new EnvironmentDocument { SecretsProvider = spec.Text };
        if (spec.Kind == SecretsProviderKind.Passphrase)
        {
            PassphraseSecretsProvider.Initialize(document, passphrase.GetNewPassphrase(), kdf);
        }

        environments.Save(name, document);
        return new(environments, name, document, passphrase, keyVault, vaultHandler);
    }

    /// <summary>
    /// Opens an existing environment.
    /// </summary>
    /// <param name="environments">The project environments.</param>
    /// <param name="name">The environment name.</param>
    /// <param name="passphrase">The passphrase source.</param>
    /// <param name="keyVault">Optional Key Vault access, defaulting to <see cref="AzureKeyVaultSecrets"/>.</param>
    /// <param name="vaultHandler">Optional HTTP handler for HashiCorp Vault.</param>
    /// <returns>The environment.</returns>
    public static ProjectEnvironment Open(
        ProjectEnvironments environments,
        string name,
        IPassphraseSource passphrase,
        IKeyVaultSecrets? keyVault = null,
        HttpMessageHandler? vaultHandler = null) =>
        new(environments, name, environments.Load(name), passphrase, keyVault, vaultHandler);

    /// <summary>
    /// Gets the entry stored for a key, without resolving it.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The entry, or null.</returns>
    public ConfigEntry? GetEntry(string key) => Document.Config.GetValueOrDefault(key);

    /// <summary>
    /// Resolves the value of a key, decrypting or fetching secrets.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The value, or null when the key is not set.</returns>
    public async Task<string?> GetValueAsync(string key, CancellationToken cancellationToken = default)
    {
        var entry = GetEntry(key);
        if (entry is null)
        {
            return null;
        }

        return entry.Kind == ConfigEntryKind.Plain ? entry.Value : await Provider_().ResolveAsync(key, entry, cancellationToken);
    }

    /// <summary>
    /// Resolves every value, including secrets.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The values by key.</returns>
    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var key in Keys)
        {
            result[key] = (await GetValueAsync(key, cancellationToken))!;
        }

        return result;
    }

    /// <summary>
    /// Sets a value and saves the file. A secret goes through the secrets provider and only its protected form is written.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <param name="secret">True to protect the value with the secrets provider.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task.</returns>
    public async Task SetAsync(string key, string value, bool secret, CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        var previous = GetEntry(key);
        var entry = secret ? await Provider_().StoreAsync(key, value, cancellationToken) : ConfigEntry.Plain(value);
        Document.Config[key] = entry;
        _environments.Save(Name, Document);
        if (previous is { Kind: ConfigEntryKind.Reference } && previous != entry)
        {
            await Provider_().RemoveAsync(key, previous, cancellationToken);
        }
    }

    /// <summary>
    /// Removes a key and any secret material stored for it, then saves the file.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>True when the key existed.</returns>
    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (!Document.Config.Remove(key, out var entry))
        {
            return false;
        }

        _environments.Save(Name, Document);
        if (entry.Kind == ConfigEntryKind.Reference)
        {
            await Provider_().RemoveAsync(key, entry, cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Moves every secret to another secrets provider and removes the previous material. No clear text is written.
    /// </summary>
    /// <param name="providerSpec">The new provider specification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of secrets moved.</returns>
    public async Task<int> ChangeSecretsProviderAsync(string providerSpec, CancellationToken cancellationToken = default)
    {
        var spec = SecretsProviderSpec.Parse(providerSpec);
        if (spec.Kind == SecretsProviderKind.Passphrase && Provider.Kind == SecretsProviderKind.Passphrase)
        {
            return await ChangePassphraseAsync(cancellationToken);
        }

        var target = new EnvironmentDocument { SecretsProvider = spec.Text };
        var targetPassphrase = _passphrase;
        if (spec.Kind == SecretsProviderKind.Passphrase)
        {
            var newPassphrase = _passphrase.GetNewPassphrase();
            PassphraseSecretsProvider.Initialize(target, newPassphrase);
            targetPassphrase = PassphraseSources.Fixed(newPassphrase);
        }

        return await MoveAsync(target, targetPassphrase, cancellationToken);
    }

    /// <summary>
    /// Re-encrypts every secret under a new passphrase and a new salt.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of secrets re-encrypted.</returns>
    /// <exception cref="EnvironmentException">Thrown when the environment does not use the passphrase provider.</exception>
    public async Task<int> ChangePassphraseAsync(CancellationToken cancellationToken = default)
    {
        if (Provider.Kind != SecretsProviderKind.Passphrase)
        {
            throw new EnvironmentException("Only environments using the passphrase secrets provider have a passphrase.");
        }

        var newPassphrase = _passphrase.GetNewPassphrase();
        var target = new EnvironmentDocument { SecretsProvider = EnvironmentDocument.PassphraseProvider };
        PassphraseSecretsProvider.Initialize(target, newPassphrase, Document.Kdf);
        return await MoveAsync(target, PassphraseSources.Fixed(newPassphrase), cancellationToken);
    }

    static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Any(char.IsControl))
        {
            throw new EnvironmentException("A configuration key must not be empty or contain control characters.");
        }
    }

    ISecretsProvider Provider_() => _provider ??= SecretsProviders.Create(Name, Document, _passphrase, _keyVault, _vaultHandler);

    async Task<int> MoveAsync(EnvironmentDocument target, IPassphraseSource targetPassphrase, CancellationToken cancellationToken)
    {
        var current = Provider_();
        var next = SecretsProviders.Create(Name, target, targetPassphrase, _keyVault, _vaultHandler);
        var moved = 0;
        foreach (var (key, entry) in Document.Config)
        {
            if (!entry.IsSecret)
            {
                target.Config[key] = entry;
                continue;
            }

            var value = await current.ResolveAsync(key, entry, cancellationToken);
            target.Config[key] = await next.StoreAsync(key, value, cancellationToken);
            moved++;
        }

        var previous = Document;
        _environments.Save(Name, target);
        Document = target;
        _provider = next;

        foreach (var (key, entry) in previous.Config.Where(pair => pair.Value.Kind == ConfigEntryKind.Reference))
        {
            if (target.Config.GetValueOrDefault(key) != entry)
            {
                await current.RemoveAsync(key, entry, cancellationToken);
            }
        }

        return moved;
    }
}
