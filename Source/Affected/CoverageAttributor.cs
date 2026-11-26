// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Attributes coverage to test units by running them serially, each unit or each namespace group in its own process under a coverage collector.
/// Coverage tools report per run, not per test, so isolation is what makes the attribution exact and repeatable.
/// </summary>
/// <param name="runner">The runner of the test project.</param>
/// <param name="git">The repository.</param>
/// <param name="collector">The coverage collector executable: dotnet-coverage by default, or coverlet.</param>
/// <param name="parallelism">How many coverage runs may run at once. coverlet rewrites the assemblies it measures, so it always runs one.</param>
public class CoverageAttributor(ITestRunner runner, IGitRepository git, string collector = "dotnet-coverage", int parallelism = 1)
{
    /// <summary>
    /// Runs every unit of a test project and builds its coverage map.
    /// </summary>
    /// <param name="assembly">The built test assembly.</param>
    /// <param name="project">The test project.</param>
    /// <param name="commit">The commit under test, recorded in the map.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <param name="grouping">How many units share one coverage run.</param>
    /// <param name="maxGroups">The most coverage runs for the project, or 0 for no limit.</param>
    /// <param name="workDirectory">A directory for the temporary coverage reports.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The map and whether every unit passed.</returns>
    public async Task<(CoverageMap Map, bool AllPassed)> AttributeAsync(
        TestAssembly assembly,
        ProjectDefinition project,
        string commit,
        UnitGranularity granularity,
        AttributionGroup grouping,
        int maxGroups,
        string workDirectory,
        CancellationToken cancellationToken)
    {
        var units = await runner.DiscoverUnitsAsync(assembly, granularity, cancellationToken);
        var tracked = git.ListFiles(commit).ToHashSet(StringComparer.Ordinal);
        var declarations = DeclarationFinder.FindInProject(git.Root, project, tracked);
        Directory.CreateDirectory(workDirectory);

        var groups = GroupUnits(units, granularity, grouping, maxGroups);

        var isCoverlet = Path.GetFileNameWithoutExtension(collector).StartsWith("coverlet", StringComparison.OrdinalIgnoreCase);
        var degree = isCoverlet ? 1 : Math.Max(1, parallelism);
        var covered = new IReadOnlySet<string>[groups.Count];
        var passed = new bool[groups.Count];

        await Parallel.ForEachAsync(
            Enumerable.Range(0, groups.Count),
            new ParallelOptions { MaxDegreeOfParallelism = degree, CancellationToken = cancellationToken },
            async (index, token) =>
            {
                var report = Path.Combine(workDirectory, $"group-{index}.cobertura.xml");
                File.Delete(report);
                var filter = groups[index].SelectMany(unit => runner.CreateUnitFilter(unit, granularity)).ToList();
                var command = CollectorArguments(assembly, runner.GetCommand(assembly, filter), report);
                var exit = 0;
                for (var attempt = 1; attempt <= 2; attempt++)
                {
                    if (degree == 1)
                    {
                        exit = await ProcessRunner.RunInheritedAsync(collector, command, git.Root, token);
                    }
                    else
                    {
                        // Output of concurrent runs would interleave, so it is kept and shown only for a run that failed.
                        var result = await Task.Run(() => ProcessRunner.Run(collector, command, git.Root), token);
                        exit = result.ExitCode;
                        if (exit != 0)
                        {
                            await Console.Out.WriteLineAsync($"::group::{groups[index][0]} (and {groups[index].Count - 1} more) exited with {exit} on attempt {attempt}\n{result.Output}\n{result.Error}\n::endgroup::");
                        }
                    }

                    // A run that failed may have stopped early, so its coverage is partial; one retry absorbs a flake.
                    if (exit == 0)
                    {
                        break;
                    }

                    File.Delete(report);
                }

                passed[index] = exit == 0;
                covered[index] = File.Exists(report)
                    ? CoberturaReader.Files(await File.ReadAllTextAsync(report, token), git.Root, tracked)
                    : new HashSet<string>(StringComparer.Ordinal);
                File.Delete(report);
            });

        var collected = new List<(string, IEnumerable<string>, IEnumerable<string>)>();
        for (var index = 0; index < groups.Count; index++)
        {
            foreach (var unit in groups[index])
            {
                var className = granularity == UnitGranularity.Test ? UnitNames.ClassOf(unit) : unit;
                declarations.TryGetValue(className, out var declaredIn);
                collected.Add((unit, covered[index], declaredIn ?? []));
            }
        }

        var allPassed = passed.All(ok => ok);
        return (CoverageMap.Create(commit, project.Path, granularity, collected), allPassed);
    }

    /// <summary>
    /// Groups the units of a project into coverage runs.
    /// </summary>
    /// <param name="units">The units.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <param name="grouping">How units share a run.</param>
    /// <param name="maxGroups">The most runs, or 0 for no limit.</param>
    /// <returns>The units of each run.</returns>
    public static List<List<string>> GroupUnits(IReadOnlyList<string> units, UnitGranularity granularity, AttributionGroup grouping, int maxGroups)
    {
        if (grouping != AttributionGroup.Namespace)
        {
            return [.. units.Select(unit => new List<string> { unit })];
        }

        var byNamespace = units
            .GroupBy(unit => UnitNames.GroupOf(unit, granularity), StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => group.ToList())
            .ToList();
        return BoundGroups(byNamespace, maxGroups);
    }

    static List<List<string>> BoundGroups(List<List<string>> groups, int maxGroups)
    {
        if (maxGroups <= 0 || groups.Count <= maxGroups)
        {
            return groups;
        }

        var total = groups.Sum(group => group.Count);
        var target = (double)total / maxGroups;
        var bounded = new List<List<string>>();
        var current = new List<string>();
        foreach (var group in groups)
        {
            current.AddRange(group);
            if (current.Count >= target && bounded.Count < maxGroups - 1)
            {
                bounded.Add(current);
                current = [];
            }
        }

        if (current.Count > 0)
        {
            bounded.Add(current);
        }

        return bounded;
    }

    static string Quote(string argument) =>
        argument.Length > 0 && argument.All(c => char.IsLetterOrDigit(c) || "/._-:+=".Contains(c, StringComparison.Ordinal))
            ? argument
            : "\"" + argument.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"";

    IReadOnlyList<string> CollectorArguments(TestAssembly assembly, IReadOnlyList<string> command, string report)
    {
        // coverlet instruments the assemblies next to the test assembly and then runs the target; dotnet-coverage attaches a profiler to the command.
        if (Path.GetFileNameWithoutExtension(collector).StartsWith("coverlet", StringComparison.OrdinalIgnoreCase))
        {
            return [assembly.AssemblyPath, "--target", command[0], "--targetargs", string.Join(' ', command.Skip(1).Select(Quote)), "--format", "cobertura", "--output", report];
        }

        return ["collect", "--output", report, "--output-format", "cobertura", "--nologo", string.Join(' ', command.Select(Quote))];
    }
}
