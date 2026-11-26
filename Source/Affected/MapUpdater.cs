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
    /// <param name="projects">The test projects to update, or none for all of them.</param>
    /// <param name="cancellationToken">Cancels the update.</param>
    /// <returns>What changed.</returns>
    /// <exception cref="AffectedException">A named project is not a test project.</exception>
    public async Task<MapUpdateResult> UpdateAsync(string commit, string workDirectory, IReadOnlyCollection<string>? projects = null, CancellationToken cancellationToken = default)
    {
        var configuration = AffectedConfiguration.Load(git.Root);
        var resolved = git.ResolveCommit(commit);
        var graph = ProjectGraph.Load(git, resolved, configuration);
        var written = new List<string>();
        var failed = new List<string>();

        var only = projects is { Count: > 0 } ? projects.ToHashSet(StringComparer.Ordinal) : null;
        if (only is not null)
        {
            var unknown = only.Where(path => graph.Projects.GetValueOrDefault(path)?.IsTestProject != true).Order(StringComparer.Ordinal).ToList();
            if (unknown.Count > 0)
            {
                throw new AffectedException($"Not test projects of {commit}: {string.Join(", ", unknown)}");
            }
        }

        foreach (var project in graph.Projects.Values.Where(project => project.IsTestProject && only?.Contains(project.Path) != false).OrderBy(project => project.Path, StringComparer.Ordinal))
        {
            TestAssembly assembly;
            try
            {
                assembly = await runner.BuildAsync(project.Path, cancellationToken);
            }
            catch (AffectedException exception)
            {
                // A project that does not build keeps its old map and must not stop the maps of the others.
                await Console.Error.WriteLineAsync($"::error title=affected-tests::{project.Path} could not be built: {exception.Message}");
                failed.Add(project.Path);
                continue;
            }

            var (map, allPassed) = await attributor.AttributeAsync(
                assembly,
                project,
                resolved,
                configuration.Granularity,
                configuration.AttributionGroup,
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

        // A run of some projects must not delete the maps of the others, which another run writes.
        return new MapUpdateResult(written, only is null ? DeleteStaleMaps(graph) : [], failed);
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
