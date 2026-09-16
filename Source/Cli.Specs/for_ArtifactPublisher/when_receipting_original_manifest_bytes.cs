// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_receipting_original_manifest_bytes : given.an_artifact_publication
{
    [Theory]
    [InlineData(65001)]
    [InlineData(1200)]
    [InlineData(1201)]
    public async Task should_hash_the_same_raw_snapshot_that_is_decoded_and_parsed(int codePage)
    {
        await Publish();
        var manifestPath = ArtifactPublicationStorage.ManifestPath(_destination);
        var canonicalBytes = await File.ReadAllBytesAsync(manifestPath);
        var manifest = ArtifactPublicationStorage.ReadManifest(_destination);
        var formatted = " \r\n" + JsonSerializer.Serialize(manifest, JsonSerializerOptions.Web) + "\r\n ";
        var encoding = Encoding.GetEncoding(codePage);
        byte[] original = [.. encoding.GetPreamble(), .. encoding.GetBytes(formatted)];
        await File.WriteAllBytesAsync(manifestPath, original);
        var beforeHash = Convert.ToHexString(SHA256.HashData(original)).ToLowerInvariant();

        var result = await Publish();

        result.Receipt.Manifest.BaseSha256.ShouldEqual(beforeHash);
        result.Receipt.Manifest.Sha256.ShouldNotEqual(beforeHash);
        result.Receipt.Manifest.Sha256.ShouldEqual(Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(manifestPath))).ToLowerInvariant());
        (await File.ReadAllBytesAsync(manifestPath)).SequenceEqual(canonicalBytes).ShouldBeTrue();
        result.Receipt.Changes.ShouldBeEmpty();
        result.Written.ShouldEqual(0);
        result.Removed.ShouldEqual(0);
    }
}
