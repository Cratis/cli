// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render.Publication;

internal sealed record ArtifactSourcePaths(string Id, string Kind, string? Address, IReadOnlyList<string> Paths)
{
    public static IReadOnlyList<ArtifactSourcePaths> Group(IEnumerable<(string Path, IReadOnlyList<ArtifactSource> Sources)> artifacts) =>
        [.. artifacts.SelectMany(artifact => artifact.Sources.Select(source => (artifact.Path, Source: source)))
            .GroupBy(artifact => artifact.Source.Id, StringComparer.Ordinal)
            .Select(group => new ArtifactSourcePaths(
                group.Key,
                group.First().Source.Kind,
                group.First().Source.Address,
                [.. group.Select(artifact => artifact.Path).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)]))
            .OrderBy(source => source.Address is null)
            .ThenBy(source => source.Address, StringComparer.Ordinal)
            .ThenBy(source => source.Id, StringComparer.Ordinal)];
}
