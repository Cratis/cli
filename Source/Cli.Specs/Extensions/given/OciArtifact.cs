// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cratis.Cli.Extensions;

/// <summary>
/// Publishes an OCI artifact to a <see cref="FakeRegistryServer"/> the way a package build would.
/// </summary>
static class OciArtifact
{
    public const string Repository = "cratis/extension-stripe";

    public static string Publish(FakeRegistryServer server, string version, string contract, string? publisher = "https://github.com/cratis", ECDsa? key = null)
    {
        var tar = Tar("extension/contract.play", contract);
        var config = "{}"u8.ToArray();
        var manifest = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = 2,
            mediaType = "application/vnd.oci.image.manifest.v1+json",
            config = new { mediaType = "application/vnd.oci.image.config.v1+json", digest = FakeRegistryServer.DigestOf(config), size = config.Length },
            layers = new[] { new { mediaType = "application/vnd.oci.image.layer.v1.tar+gzip", digest = FakeRegistryServer.DigestOf(tar), size = tar.Length } }
        });
        var digest = FakeRegistryServer.DigestOf(manifest);
        server
            .Serve($"/v2/{Repository}/manifests/{version}", "application/vnd.oci.image.manifest.v1+json", manifest)
            .Serve($"/v2/{Repository}/blobs/{FakeRegistryServer.DigestOf(config)}", "application/octet-stream", config)
            .Serve($"/v2/{Repository}/blobs/{FakeRegistryServer.DigestOf(tar)}", "application/octet-stream", tar);
        if (publisher is not null) Sign(server, digest, publisher, key ?? ECDsa.Create(ECCurve.NamedCurves.nistP256));
        return digest;
    }

    public static void ListTags(FakeRegistryServer server, params string[] tags) =>
        server.Serve($"/v2/{Repository}/tags/list", JsonSerializer.Serialize(new { name = Repository, tags }));

    static void Sign(FakeRegistryServer server, string digest, string publisher, ECDsa key)
    {
        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            critical = new { image = new Dictionary<string, string> { ["docker-manifest-digest"] = digest } },
            optional = new { publisher, key = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()) }
        }));
        var signature = key.SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
        var config = "{}"u8.ToArray();
        var manifest = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = 2,
            mediaType = "application/vnd.oci.image.manifest.v1+json",
            config = new { mediaType = "application/vnd.oci.image.config.v1+json", digest = FakeRegistryServer.DigestOf(config), size = config.Length },
            layers = new[]
            {
                new
                {
                    mediaType = "application/vnd.dev.cosign.simplesigning.v1+json",
                    digest = FakeRegistryServer.DigestOf(payload),
                    size = payload.Length,
                    annotations = new Dictionary<string, string> { ["dev.cosignproject.cosign/signature"] = Convert.ToBase64String(signature) }
                }
            }
        });
        server
            .Serve($"/v2/{Repository}/manifests/{digest.Replace(':', '-')}.sig", "application/vnd.oci.image.manifest.v1+json", manifest)
            .Serve($"/v2/{Repository}/blobs/{FakeRegistryServer.DigestOf(payload)}", "application/octet-stream", payload);
    }

    static byte[] Tar(string name, string content)
    {
        using var memory = new MemoryStream();
        using (var gzip = new GZipStream(memory, CompressionLevel.Fastest, true))
        using (var writer = new TarWriter(gzip, true))
        {
            writer.WriteEntry(new UstarTarEntry(TarEntryType.RegularFile, name) { DataStream = new MemoryStream(Encoding.UTF8.GetBytes(content)) });
        }

        return memory.ToArray();
    }
}
