// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Which source files each test unit of one test project executed, as of one commit.
/// </summary>
/// <param name="SchemaVersion">The version of the file format.</param>
/// <param name="BuiltAt">The commit the map was built from.</param>
/// <param name="Project">The repository relative path of the test project file.</param>
/// <param name="Granularity">Whether units are test classes or individual tests.</param>
/// <param name="Files">The sorted file table.</param>
/// <param name="Units">Every known unit, sorted by name, including those that covered nothing.</param>
public record CoverageMap(
    int SchemaVersion,
    string BuiltAt,
    string Project,
    UnitGranularity Granularity,
    IReadOnlyList<string> Files,
    IReadOnlyList<CoverageUnit> Units)
{
    /// <summary>
    /// The schema version this build reads and writes.
    /// </summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Builds a map from plain file names, assigning the indexes and sorting everything so equal input gives equal output.
    /// </summary>
    /// <param name="builtAt">The commit the data was collected at.</param>
    /// <param name="project">The test project file path.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <param name="units">Each unit with the files it covered and the files that declare it.</param>
    /// <returns>The map.</returns>
    public static CoverageMap Create(
        string builtAt,
        string project,
        UnitGranularity granularity,
        IEnumerable<(string Name, IEnumerable<string> Covers, IEnumerable<string> DeclaredIn)> units)
    {
        var list = units.Select(unit => (unit.Name, Covers: unit.Covers.ToHashSet(StringComparer.Ordinal), DeclaredIn: unit.DeclaredIn.ToHashSet(StringComparer.Ordinal)))
            .OrderBy(unit => unit.Name, StringComparer.Ordinal)
            .ToList();
        var files = list.SelectMany(unit => unit.Covers.Union(unit.DeclaredIn)).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
        var index = files.Select((file, position) => (file, position)).ToDictionary(pair => pair.file, pair => pair.position, StringComparer.Ordinal);

        return new CoverageMap(
            CurrentSchemaVersion,
            builtAt,
            project,
            granularity,
            files,
            [.. list.Select(unit => new CoverageUnit(
                unit.Name,
                [.. unit.Covers.Select(file => index[file]).Order()],
                [.. unit.DeclaredIn.Select(file => index[file]).Order()]))]);
    }

    /// <summary>
    /// Merges another map into this one, unioning the coverage of units both know and keeping units only one knows.
    /// </summary>
    /// <param name="previous">The earlier map of the same project.</param>
    /// <param name="existsNow">Tells whether a file still exists, so coverage of deleted files is dropped.</param>
    /// <returns>The merged map, built at the commit of this one.</returns>
    public CoverageMap UnionWith(CoverageMap previous, Func<string, bool> existsNow)
    {
        var merged = new Dictionary<string, (HashSet<string> Covers, HashSet<string> DeclaredIn)>(StringComparer.Ordinal);
        foreach (var unit in Units)
        {
            merged[unit.Name] = ([.. unit.Covers.Select(i => Files[i])], [.. unit.DeclaredIn.Select(i => Files[i])]);
        }

        foreach (var unit in previous.Units.Where(unit => merged.ContainsKey(unit.Name)))
        {
            merged[unit.Name].Covers.UnionWith(unit.Covers.Select(i => previous.Files[i]).Where(existsNow));
        }

        return Create(BuiltAt, Project, Granularity, merged.Select(pair => (pair.Key, (IEnumerable<string>)pair.Value.Covers, (IEnumerable<string>)pair.Value.DeclaredIn)));
    }
}
