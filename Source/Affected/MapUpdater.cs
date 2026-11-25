// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// The merge-time full run: runs every test with attribution, writes one map per test project and removes maps of projects that are gone.
/// Maps are never written from pull requests.
/// </summary>
/// <param name="git">The repository.</param>
/// <param name="runner">The test runner strategy.</param>
/// <param name="attributor">The coverage attributor.</param>
public class MapUpdater(IGitRepository git, ITestRunner runner, CoverageAttributor attributor)
{
    /// <summary>
    /// Updates the maps of every test project.
    /// </summary>
    /// <param name="commit">The commit under test.</param>
    /// <param name="workDirectory">A directory for temporary coverage reports.</param>
    /// <param name="cancellationToken">Cancels the update.</param>
    /// <returns>What changed.</returns>
    public async Task<MapUpdateResult> UpdateAsync(string commit, string workDirectory, CancellationToken cancellationToken = default)
    {
        var configuration = AffectedConfiguration.Load(git.Root);
        var resolved = git.ResolveCommit(commit);
        var graph = ProjectGraph.Load(git, resolved, configuration);
        var written = new List<string>();
        var failed = new List<string>();

        foreach (var project in graph.Projects.Values.Where(project => project.IsTestProject).OrderBy(project => project.Path, StringComparer.Ordinal))
        {
            var assembly = await runner.BuildAsync(project.Path, cancellationToken);
            var (map, allPassed) = await attributor.AttributeAsync(
                assembly,
                project,
                resolved,
                configuration.Granularity,
                Path.Combine(workDirectory, project.Path.Replace('/', '_')),
                cancellationToken);
            if (!allPassed)
            {
                failed.Add(project.Path);
                continue;
            }

            if (configuration.UnionPreviousMap && CoverageMapFile.Read(git.Root, project.Path) is { } previous)
            {
                map = map.UnionWith(previous, file => File.Exists(Path.Combine(git.Root, file)));
            }

            if (CoverageMapFile.Write(git.Root, map))
            {
                written.Add(CoverageMapFile.PathFor(project.Path));
            }
        }

        return new MapUpdateResult(written, DeleteStaleMaps(graph), failed);
    }

    List<string> DeleteStaleMaps(ProjectGraph graph)
    {
        var deleted = new List<string>();
        var folder = Path.Combine(git.Root, AffectedConfiguration.MapsFolder);
        if (!Directory.Exists(folder))
        {
            return deleted;
        }

        var live = graph.Projects.Values.Where(project => project.IsTestProject).Select(project => CoverageMapFile.PathFor(project.Path)).ToHashSet(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var relative = Path.GetRelativePath(git.Root, file).Replace('\\', '/');
            if (!live.Contains(relative))
            {
                File.Delete(file);
                deleted.Add(relative);
            }
        }

        return deleted;
    }
}
