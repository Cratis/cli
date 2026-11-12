// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Builds signed and unsigned packages for the extension specifications.
/// </summary>
static class ExtensionPackageFactory
{
    public const string StripeResolved = "ghcr.io/cratis/extension-stripe";

    static readonly ConcurrentDictionary<string, ECDsa> _keys = new();

    public static byte[] PublicKeyOf(string publisher) => KeyOf(publisher).ExportSubjectPublicKeyInfo();

    public static string FingerprintOf(string publisher) => SignatureEvidence.FingerprintOf(PublicKeyOf(publisher));

    public static RegistryPackage Signed(string version, string contract, string publisher = "https://github.com/cratis", string resolved = StripeResolved) =>
        With(version, contract, resolved, publisher, true);

    public static RegistryPackage Unsigned(string version, string contract, string resolved = StripeResolved) =>
        With(version, contract, resolved, null, false);

    public static RegistryPackage Forged(string version, string contract, string publisher = "https://github.com/cratis", string resolved = StripeResolved)
    {
        var package = Signed(version, contract, publisher, resolved);
        return package with { Signature = package.Signature! with { Signature = new byte[64] } };
    }

    public static RegistryPackage Tampered(RegistryPackage package, string contract) => package with { Digest = "sha256:" + new string('a', 64), Contract = Encoding.UTF8.GetBytes(contract) };

    static RegistryPackage With(string version, string contract, string resolved, string? publisher, bool signed)
    {
        var contractBytes = new UTF8Encoding(false).GetBytes(contract);
        var manifest = Encoding.UTF8.GetBytes($"{{\"version\":\"{version}\",\"contract\":\"{Convert.ToHexString(SHA256.HashData(contractBytes))}\"}}");
        var digest = "sha256:" + Convert.ToHexString(SHA256.HashData(manifest)).ToLowerInvariant();
        SignatureEvidence? evidence = null;
        if (signed)
        {
            var payload = Encoding.UTF8.GetBytes($"{{\"critical\":{{\"image\":{{\"docker-manifest-digest\":\"{digest}\"}}}},\"optional\":{{\"publisher\":\"{publisher}\"}}}}");
            evidence = new(publisher!, SignatureAnchor.Embedded, PublicKeyOf(publisher!), payload, KeyOf(publisher!).SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence));
        }

        return new(ExtensionPackageKind.Oci, resolved, version, digest, contractBytes, [new("manifest.json", "application/vnd.oci.image.manifest.v1+json", manifest)], evidence);
    }

    static ECDsa KeyOf(string publisher) => _keys.GetOrAdd(publisher, _ => ECDsa.Create(ECCurve.NamedCurves.nistP256));
}
