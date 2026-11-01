// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Environments.Secrets;

/// <param name="document">The environment document, which carries the salt, KDF parameters and verification value.</param>
/// <param name="passphrase">The passphrase source.</param>
/// <summary>
/// Encrypts secrets inline with AES-256-GCM, using a key derived from a passphrase and the random per-environment salt in the file.
/// </summary>
public class PassphraseSecretsProvider(EnvironmentDocument document, IPassphraseSource passphrase) : ISecretsProvider
{
    const int KeySize = 32;
    const int NonceSize = 12;
    const int TagSize = 16;
    const int SaltSize = 32;
    const string CheckPlaintext = "cratis-environment-passphrase-check";
    const string CheckContext = "\0check";

    byte[]? _key;

    /// <summary>
    /// Prepares a document for the passphrase provider: a fresh random salt, the KDF parameters and the verification value.
    /// Existing entries are untouched, so callers re-encrypt them afterwards.
    /// </summary>
    /// <param name="document">The document to prepare.</param>
    /// <param name="passphrase">The new passphrase.</param>
    /// <param name="kdf">Optional KDF parameters, defaulting to <see cref="KdfParameters.Default"/>.</param>
    public static void Initialize(EnvironmentDocument document, string passphrase, KdfParameters? kdf = null)
    {
        document.SecretsProvider = EnvironmentDocument.PassphraseProvider;
        document.Kdf = kdf ?? KdfParameters.Default;
        document.EncryptionSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSize));
        var key = Derive(passphrase, document);
        document.EncryptionCheck = Encrypt(key, CheckPlaintext, CheckContext);
    }

    /// <inheritdoc/>
    public Task<ConfigEntry> StoreAsync(string key, string value, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ConfigEntry(ConfigEntryKind.Secure, Encrypt(Key(), value, key)));

    /// <inheritdoc/>
    public Task<string> ResolveAsync(string key, ConfigEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Kind != ConfigEntryKind.Secure)
        {
            throw new SecretNotAvailableException($"'{key}' is not stored by the passphrase provider.");
        }

        try
        {
            return Task.FromResult(Decrypt(Key(), entry.Value, key));
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            throw new SecretNotAvailableException($"The secret '{key}' could not be decrypted. The environment file may have been modified.", ex);
        }
    }

    /// <inheritdoc/>
    public Task RemoveAsync(string key, ConfigEntry entry, CancellationToken cancellationToken = default) => Task.CompletedTask;

    static byte[] Derive(string passphrase, EnvironmentDocument document)
    {
        if (document.EncryptionSalt is null || document.Kdf is null)
        {
            throw new EnvironmentException("The environment has no encryption salt or key derivation parameters. Recreate it with: cratis env init <name>");
        }

        if (!string.Equals(document.Kdf.Algorithm, KdfParameters.Pbkdf2Sha256, StringComparison.Ordinal))
        {
            throw new EnvironmentException($"The key derivation algorithm '{document.Kdf.Algorithm}' is not supported by this version.");
        }

        var salt = Convert.FromBase64String(document.EncryptionSalt);
        return Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(passphrase), salt, document.Kdf.Iterations, HashAlgorithmName.SHA256, KeySize);
    }

    static string Encrypt(byte[] key, string plaintext, string context)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var data = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[data.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, data, cipher, tag, Encoding.UTF8.GetBytes(context));
        return Convert.ToBase64String([.. nonce, .. tag, .. cipher]);
    }

    static string Decrypt(byte[] key, string payload, string context)
    {
        var bytes = Convert.FromBase64String(payload);
        if (bytes.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("The ciphertext is too short.");
        }

        var nonce = bytes.AsSpan(0, NonceSize);
        var tag = bytes.AsSpan(NonceSize, TagSize);
        var cipher = bytes.AsSpan(NonceSize + TagSize);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain, Encoding.UTF8.GetBytes(context));
        return Encoding.UTF8.GetString(plain);
    }

    byte[] Key()
    {
        if (_key is not null)
        {
            return _key;
        }

        var key = Derive(passphrase.GetPassphrase(), document);
        if (document.EncryptionCheck is null)
        {
            throw new EnvironmentException("The environment has no passphrase verification value. Recreate it with: cratis env init <name>");
        }

        try
        {
            if (!string.Equals(Decrypt(key, document.EncryptionCheck, CheckContext), CheckPlaintext, StringComparison.Ordinal))
            {
                throw new InvalidPassphraseException();
            }
        }
        catch (CryptographicException)
        {
            throw new InvalidPassphraseException();
        }

        return _key = key;
    }
}
