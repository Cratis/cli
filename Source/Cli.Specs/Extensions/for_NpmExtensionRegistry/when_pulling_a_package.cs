// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Cli.Extensions.for_NpmExtensionRegistry;

public class when_pulling_a_package : Specification
{
    const string Name = "@acme/charts";
    const string Base = "https://registry.npmjs.org";

    FakeRegistryServer _server;
    NpmExtensionRegistry _registry;
    IReadOnlyList<string> _versions;
    RegistryPackage _package;
    RegistryPackage _missing;
    Exception _tampered;
    byte[] _tarball;

    static byte[] Tarball(string contract)
    {
        using var memory = new MemoryStream();
        using (var gzip = new GZipStream(memory, CompressionLevel.Fastest, true))
        using (var writer = new TarWriter(gzip, true))
        {
            writer.WriteEntry(new UstarTarEntry(TarEntryType.RegularFile, "package/contract.play") { DataStream = new MemoryStream(Encoding.UTF8.GetBytes(contract)) });
        }

        return memory.ToArray();
    }

    void Establish()
    {
        _server = new();
        _tarball = Tarball(ExtensionContracts.Stripe2);
        var integrity = "sha512-" + Convert.ToBase64String(SHA512.HashData(_tarball));
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var signature = key.SignData(Encoding.UTF8.GetBytes($"{Name}@1.2.0:{integrity}"), HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        _server
            .Serve("/@acme%2Fcharts", JsonSerializer.Serialize(new
            {
                versions = new Dictionary<string, object>
                {
                    ["1.2.0"] = new { dist = new { tarball = Base + "/@acme/charts/-/charts-1.2.0.tgz", integrity, signatures = new[] { new { keyid = "SHA256:npm-key", sig = Convert.ToBase64String(signature) } } } },
                    ["1.3.0"] = new { dist = new { tarball = Base + "/@acme/charts/-/charts-1.3.0.tgz", integrity } }
                }
            }))
            .Serve("/-/npm/v1/keys", JsonSerializer.Serialize(new { keys = new[] { new { keyid = "SHA256:npm-key", key = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()) } } }))
            .Serve("/@acme/charts/-/charts-1.2.0.tgz", "application/gzip", _tarball)
            .Serve("/@acme/charts/-/charts-1.3.0.tgz", "application/gzip", [1, 2, 3]);
        _registry = new(new RegistryHttp(new HttpClient(_server), new FixedCredentials()));
    }

    async Task Because()
    {
        _versions = await _registry.Versions("npm:" + Name, CancellationToken.None);
        _package = (await _registry.Pull("npm:" + Name, "1.2.0", CancellationToken.None))!;
        _missing = await _registry.Pull("npm:@acme/other", "1.0.0", CancellationToken.None);
        _tampered = await Catch.Exception(() => _registry.Pull("npm:" + Name, "1.3.0", CancellationToken.None));
    }

    [Fact] void should_list_the_versions() => _versions.Order().ShouldEqual(["1.2.0", "1.3.0"]);
    [Fact] void should_use_the_tarball_hash_as_the_digest() => _package.Digest.ShouldEqual("sha256:" + Convert.ToHexString(SHA256.HashData(_tarball)).ToLowerInvariant());
    [Fact] void should_read_the_contract_at_the_package_root() => Encoding.UTF8.GetString(_package.Contract).ShouldEqual(ExtensionContracts.Stripe2);
    [Fact] void should_name_the_scope_as_the_publisher() => _package.Signature!.Publisher.ShouldEqual("npm:@acme");
    [Fact] void should_let_the_registry_vouch_for_the_key() => _package.Signature!.Anchor.ShouldEqual(SignatureAnchor.Registry);
    [Fact] void should_verify_the_registry_signature() => (_package.Signature!.Verifies() && _package.Signature.BindsTo(_package)).ShouldBeTrue();
    [Fact] void should_return_nothing_for_a_package_the_registry_does_not_have() => _missing.ShouldBeNull();
    [Fact] void should_refuse_a_tarball_that_does_not_match_the_published_integrity() => _tampered.ShouldBeOfExactType<ExtensionFailure>();
}
