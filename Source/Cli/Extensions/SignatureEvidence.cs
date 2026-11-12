// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Defines who vouches for the public key that verified a signature.
/// </summary>
public enum SignatureAnchor
{
    /// <summary>The package carries its key; only the project's allow-list can vouch for it.</summary>
    Embedded,

    /// <summary>The registry published the key; the registry vouches for it.</summary>
    Registry
}

/// <summary>
/// Represents the signature of a package: the publisher it claims, the key, the signed payload and the signature.
/// </summary>
/// <remarks>
/// An OCI package is signed with a cosign-layout signature artifact whose payload names the manifest digest and carries the publisher
/// and the key; an npm package is signed by the registry over <c>name@version:integrity</c>. Both are ECDSA P-256 over SHA-256.
/// </remarks>
/// <param name="Publisher">The identity of the publisher.</param>
/// <param name="Anchor">Who vouches for the key.</param>
/// <param name="PublicKey">The SubjectPublicKeyInfo of the key.</param>
/// <param name="Payload">The signed bytes.</param>
/// <param name="Signature">The ASN.1 DER signature.</param>
public record SignatureEvidence(string Publisher, SignatureAnchor Anchor, byte[] PublicKey, byte[] Payload, byte[] Signature)
{
    /// <summary>
    /// Gets the fingerprint of the key as <c>sha256:&lt;hex&gt;</c>.
    /// </summary>
    public string Fingerprint => FingerprintOf(PublicKey);

    /// <summary>
    /// Gets the fingerprint of a SubjectPublicKeyInfo.
    /// </summary>
    /// <param name="publicKey">The key.</param>
    /// <returns>The fingerprint.</returns>
    public static string FingerprintOf(byte[] publicKey) => "sha256:" + Convert.ToHexString(SHA256.HashData(publicKey)).ToLowerInvariant();

    /// <summary>
    /// Checks the signature over the payload with the key.
    /// </summary>
    /// <returns><see langword="true"/> when the signature is valid.</returns>
    public bool Verifies()
    {
        try
        {
            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(PublicKey, out _);
            return key.VerifyData(Payload, Signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        }
        catch (Exception exception) when (exception is CryptographicException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Checks that the signed payload is about this package and not another.
    /// </summary>
    /// <param name="package">The package.</param>
    /// <returns><see langword="true"/> when the payload names this package.</returns>
    public bool BindsTo(RegistryPackage package)
    {
        if (package.Kind == ExtensionPackageKind.Npm)
        {
            var tarball = package.Parts.FirstOrDefault(part => part.Name == NpmExtensionRegistry.TarballPart);
            if (tarball is null) return false;
            var integrity = "sha512-" + Convert.ToBase64String(SHA512.HashData(tarball.Content));
            return Encoding.UTF8.GetString(Payload) == $"{NpmExtensionRegistry.PackageName(package.Resolved)}@{package.Version}:{integrity}";
        }

        try
        {
            using var document = JsonDocument.Parse(Payload);
            return document.RootElement.GetProperty("critical").GetProperty("image").GetProperty("docker-manifest-digest").GetString() == package.Digest;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return false;
        }
    }
}
