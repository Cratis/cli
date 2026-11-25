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
public class CoverageAttributor(ITestRunner runner, IGitRepository git, string collector = "dotnet-coverage")
{
    /// <summary>
    /// Runs every unit of a test project and builds its coverage map.
    /// </summary>
    /// <param name="assembly">The built test assembly.</param>
    /// <param name="project">The test project.</param>
    /// <param name="commit">The commit under test, recorded in the map.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <param name="grouping">How many units share one coverage run.</param>
    /// <param name="workDirectory">A directory for the temporary coverage reports.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The map and whether every unit passed.</returns>
    public async Task<(CoverageMap Map, bool AllPassed)> AttributeAsync(
        TestAssembly assembly,
        ProjectDefinition project,
        string commit,
        UnitGranularity granularity,
        AttributionGroup grouping,
        string workDirectory,
        CancellationToken cancellationToken)
    {
        var units = await runner.DiscoverUnitsAsync(assembly, granularity, cancellationToken);
        var tracked = git.ListFiles(commit).ToHashSet(StringComparer.Ordinal);
        var declarations = DeclarationFinder.FindInProject(git.Root, project, tracked);
        Directory.CreateDirectory(workDirectory);

        var allPassed = true;
        var collected = new List<(string, IEnumerable<string>, IEnumerable<string>)>();
        List<List<string>> groups = grouping == AttributionGroup.Namespace
            ? [.. units.GroupBy(unit => UnitNames.GroupOf(unit, granularity), StringComparer.Ordinal).Select(group => group.ToList())]
            : [.. units.Select(unit => new List<string> { unit })];

        foreach (var group in groups)
        {
            var report = Path.Combine(workDirectory, "unit.cobertura.xml");
            File.Delete(report);
            var filter = group.SelectMany(unit => runner.CreateUnitFilter(unit, granularity)).ToList();
            var command = runner.GetCommand(assembly, filter);
            var exit = await ProcessRunner.RunInheritedAsync(collector, CollectorArguments(assembly, command, report), git.Root, cancellationToken);
            allPassed &= exit == 0;

            var covers = File.Exists(report)
                ? CoberturaReader.Files(await File.ReadAllTextAsync(report, cancellationToken), git.Root, tracked)
                : new HashSet<string>(StringComparer.Ordinal);
            foreach (var unit in group)
            {
                var className = granularity == UnitGranularity.Test ? UnitNames.ClassOf(unit) : unit;
                declarations.TryGetValue(className, out var declaredIn);
                collected.Add((unit, covers, declaredIn ?? []));
            }
        }

        return (CoverageMap.Create(commit, project.Path, granularity, collected), allPassed);
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
