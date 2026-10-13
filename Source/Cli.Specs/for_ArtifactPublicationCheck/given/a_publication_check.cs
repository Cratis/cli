// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublicationCheck.given;

public class a_publication_check : for_ArtifactPublisher.given.an_artifact_publication
{
    private protected ArtifactPublicationCheckResult _result = null!;
    protected string[] _before = [];

    private protected Task<ArtifactPublicationCheckResult> Check(bool force = false) =>
        _publisher.Check(new(_plan, _destination, force), CancellationToken.None);

    protected static string[] Snapshot(string destination) => !Directory.Exists(destination)
        ? ["absent"]
        : ["directory", .. Directory.EnumerateDirectories(destination, "*", SearchOption.AllDirectories)
            .Select(path => $"directory:{Path.GetRelativePath(destination, path)}").Order(StringComparer.Ordinal),
            .. Directory.EnumerateFiles(destination, "*", SearchOption.AllDirectories)
                .Select(path => $"file:{Path.GetRelativePath(destination, path)}:{Convert.ToHexString(File.ReadAllBytes(path))}").Order(StringComparer.Ordinal)];

    protected void AddStale(string path, bool modified)
    {
        File.WriteAllText(ArtifactPath(path), "owned stale bytes");
        var manifest = ArtifactPublicationStorage.ReadManifest(_destination)!;
        ArtifactPublicationStorage.WriteDurable(
            ArtifactPublicationStorage.ManifestPath(_destination),
            ArtifactPublicationStorage.Serialize(manifest with
            {
                Artifacts = [.. manifest.Artifacts, new ManagedArtifact(path, ArtifactPublicationStorage.Hash(ArtifactPath(path)))]
            }));
        if (modified)
        {
            File.WriteAllText(ArtifactPath(path), "user edited stale bytes");
        }
    }
}
