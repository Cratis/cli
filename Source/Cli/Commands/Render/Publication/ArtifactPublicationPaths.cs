// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render.Publication;

internal static class ArtifactPublicationPaths
{
    public static IReadOnlyList<ArtifactPublicationRefusal> Analyze(
        string destination,
        IReadOnlyList<ManagedArtifact> previous,
        IReadOnlyList<ManagedArtifact> next)
    {
        var paths = previous.Select(_ => _.Path).Concat(next.Select(_ => _.Path)).Distinct(StringComparer.Ordinal).ToArray();
        var collisions = paths.GroupBy(_ => _, StringComparer.OrdinalIgnoreCase).Where(_ => _.Count() > 1).SelectMany(_ => _).ToHashSet(StringComparer.Ordinal);
        var refusals = new List<ArtifactPublicationRefusal>();
        foreach (var path in paths.Where(path => IsReserved(path) || collisions.Contains(path)))
        {
            refusals.Add(new(path, "The manifest contains a reserved or case-colliding artifact path."));
        }

        foreach (var path in paths.Where(path => !IsReserved(path) && !collisions.Contains(path)))
        {
            var reason = ArtifactPublicationStorage.SafePathRefusal(destination, path);
            if (reason is not null)
            {
                refusals.Add(new(path, reason));
            }
        }

        return refusals;
    }

    static bool IsReserved(string path) => path.Equals(ArtifactPublicationStorage.ManifestFileName, StringComparison.OrdinalIgnoreCase) ||
        path.Equals(ArtifactPublicationStorage.ControlDirectoryName, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith($"{ArtifactPublicationStorage.ControlDirectoryName}/", StringComparison.OrdinalIgnoreCase);
}
