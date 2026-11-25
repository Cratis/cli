// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.Fixtures;

/// <summary>
/// Builds coverage maps for specs.
/// </summary>
public static class Maps
{
    public const string TestProject = "Source/Foo.Specs/Foo.Specs.csproj";

    /// <summary>
    /// A map where each unit covers the given library files and is declared in a spec file named after it.
    /// </summary>
    /// <param name="builtAt">The commit the map was built at.</param>
    /// <param name="units">Each unit with the files it covers.</param>
    /// <returns>The map.</returns>
    public static CoverageMap For(string builtAt, params (string Unit, string[] Covers)[] units) =>
        CoverageMap.Create(
            builtAt,
            TestProject,
            UnitGranularity.Class,
            units.Select(unit => (unit.Unit, (IEnumerable<string>)unit.Covers, (IEnumerable<string>)[$"Source/Foo.Specs/{unit.Unit.Split('.')[^1]}.cs"])));

    public static IReadOnlyList<FileChange> Modified(params string[] paths) => [.. paths.Select(path => new FileChange(ChangeKind.Modified, path))];

    public static ProjectSelection Select(
        CoverageMap map,
        IReadOnlyList<FileChange> changes,
        AffectedConfiguration? configuration = null,
        IReadOnlyCollection<string>? discovered = null,
        ProjectGraph? graph = null)
    {
        var projectGraph = graph ?? Projects.LibraryWithSpecs();
        return TestSelector.Select(new TestSelectionInput(
            projectGraph.Projects[TestProject],
            projectGraph,
            map,
            changes,
            discovered,
            configuration ?? new AffectedConfiguration(),
            "head"));
    }
}
