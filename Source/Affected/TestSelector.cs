// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Affected;

/// <summary>
/// Picks the tests of one project that a set of changes can reach, from its coverage map.
/// Any doubt, and any failure, resolves to running every test of the project.
/// </summary>
public static class TestSelector
{
    /// <summary>
    /// Selects tests.
    /// </summary>
    /// <param name="input">The project, its map and the changes since the map was built.</param>
    /// <returns>The decision.</returns>
    public static ProjectSelection Select(TestSelectionInput input)
    {
        try
        {
            return Compute(input);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return ProjectSelection.RunAll(input.Project.Path, $"selection failed, running everything: {exception.Message}");
        }
    }

    static ProjectSelection Compute(TestSelectionInput input)
    {
        var (project, graph, map, changes, discovered, configuration, seed) = input;
        var reasons = new List<string>();
        var known = map.Units.Select(unit => unit.Name).ToHashSet(StringComparer.Ordinal);
        var all = known.Union(discovered ?? []).ToHashSet(StringComparer.Ordinal);
        var selected = new HashSet<string>(StringComparer.Ordinal);

        var unitsByFile = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var unit in map.Units)
        {
            foreach (var file in unit.Covers.Concat(unit.DeclaredIn).Select(index => map.Files[index]))
            {
                if (!unitsByFile.TryGetValue(file, out var list))
                {
                    unitsByFile[file] = list = [];
                }

                list.Add(unit.Name);
            }
        }

        foreach (var change in changes.Where(change => !change.Paths.All(configuration.IsIgnored)))
        {
            foreach (var path in change.Paths)
            {
                if (configuration.IsRunEverythingTrigger(path))
                {
                    return ProjectSelection.RunAll(project.Path, $"{path} is a run-everything trigger", all.Count);
                }
            }

            var isNewLocation = change.Kind is ChangeKind.Added or ChangeKind.Renamed;
            var lost = change.Kind switch
            {
                ChangeKind.Modified => change.Path,
                ChangeKind.Deleted => change.Path,
                ChangeKind.Renamed => change.OldPath,
                _ => null
            };

            if (lost is not null && !SelectForLostFile(lost, change.Kind == ChangeKind.Modified, input, map, unitsByFile, selected, reasons, out var everything))
            {
                return ProjectSelection.RunAll(project.Path, everything, all.Count);
            }

            if (isNewLocation && !SelectForUnknownFile(change.Path, "added", configuration.AddedFilePolicy, input, map, unitsByFile, selected, reasons, out everything))
            {
                return ProjectSelection.RunAll(project.Path, everything, all.Count);
            }
        }

        if (discovered is not null)
        {
            foreach (var unit in discovered.Where(unit => !known.Contains(unit)).Order(StringComparer.Ordinal))
            {
                selected.Add(unit);
                reasons.Add($"{unit} is a new test");
            }
        }

        AddSample(all, known, selected, configuration.SampleRate, seed, reasons);
        selected.IntersectWith(all);

        if (selected.Count == 0)
        {
            return ProjectSelection.SkipAll(project.Path, "no changed file reaches any test of this project", all.Count);
        }

        var entries = NamespaceRollup.Apply(all, selected, map.Granularity, configuration.RollupThreshold, configuration.RollupMinUnits);
        if (entries is null)
        {
            return ProjectSelection.RunAll(project.Path, $"selection rolled up to the project root ({selected.Count} of {all.Count} units): {string.Join("; ", reasons.Take(3))}", all.Count);
        }

        var covered = all.Count(unit => entries.Any(entry => entry.Selects(unit, map.Granularity)));
        if (covered == all.Count)
        {
            return ProjectSelection.RunAll(project.Path, $"the selection covers every unit: {string.Join("; ", reasons.Take(3))}", all.Count);
        }

        return new ProjectSelection(project.Path, SelectionMode.Filtered, entries, covered, all.Count, reasons);
    }

    static bool SelectForLostFile(
        string path,
        bool stillExists,
        TestSelectionInput input,
        CoverageMap map,
        Dictionary<string, List<string>> unitsByFile,
        HashSet<string> selected,
        List<string> reasons,
        out string everything)
    {
        everything = string.Empty;
        if (!IsInsideProject(path, input.Project))
        {
            return SelectKnownOrUnknown(path, stillExists ? "changed" : "removed", input, map, unitsByFile, selected, reasons, out everything);
        }

        // A change in the test project that is not a source file, such as its project file or a resource, cannot be traced to a test.
        if (!path.EndsWith(".cs", StringComparison.Ordinal))
        {
            everything = $"{path} belongs to the test project itself and is not a source file";
            return false;
        }

        return SelectKnownOrUnknown(path, stillExists ? "changed" : "removed", input, map, unitsByFile, selected, reasons, out everything);
    }

    static bool SelectKnownOrUnknown(
        string path,
        string verb,
        TestSelectionInput input,
        CoverageMap map,
        Dictionary<string, List<string>> unitsByFile,
        HashSet<string> selected,
        List<string> reasons,
        out string everything)
    {
        if (unitsByFile.TryGetValue(path, out var units))
        {
            selected.UnionWith(units);
            reasons.Add($"{path} {verb}, covered by {units.Count} unit(s)");
            everything = string.Empty;
            return true;
        }

        return SelectForUnknownFile(path, verb, input.Configuration.UnmappedFilePolicy, input, map, unitsByFile, selected, reasons, out everything);
    }

    static bool SelectForUnknownFile(
        string path,
        string verb,
        AddedFilePolicy policy,
        TestSelectionInput input,
        CoverageMap map,
        Dictionary<string, List<string>> unitsByFile,
        HashSet<string> selected,
        List<string> reasons,
        out string everything)
    {
        everything = string.Empty;
        if (policy == AddedFilePolicy.Ignore)
        {
            reasons.Add($"{path} {verb}, not in the map, ignored by policy");
            return true;
        }

        IEnumerable<string> peers;
        if (policy == AddedFilePolicy.Directory)
        {
            var directory = PathNormalizer.DirectoryOf(path);
            peers = map.Files.Where(file => PathNormalizer.DirectoryOf(file) == directory);
        }
        else
        {
            var owners = input.Graph.OwnersOf(path);
            if (owners.Count == 0)
            {
                everything = $"{path} is not in the map and belongs to no project";
                return false;
            }

            peers = map.Files.Where(file => input.Graph.OwnersOf(file).Intersect(owners, StringComparer.Ordinal).Any());
        }

        var matched = peers.Where(unitsByFile.ContainsKey).SelectMany(file => unitsByFile[file]).ToList();
        selected.UnionWith(matched);
        reasons.Add($"{path} {verb}, not in the map: selected the {matched.Distinct(StringComparer.Ordinal).Count()} unit(s) of its {(policy == AddedFilePolicy.Directory ? "directory" : "project")}");
        return true;
    }

    static bool IsInsideProject(string path, ProjectDefinition project) => project.Contains(path);

    static void AddSample(HashSet<string> all, HashSet<string> known, HashSet<string> selected, double rate, string seed, List<string> reasons)
    {
        if (rate <= 0)
        {
            return;
        }

        var sampled = 0;
        foreach (var unit in known.Where(unit => !selected.Contains(unit)).Order(StringComparer.Ordinal))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed + "|" + unit));
            var fraction = BitConverter.ToUInt32(hash, 0) / (double)uint.MaxValue;
            if (fraction < rate && all.Contains(unit))
            {
                selected.Add(unit);
                sampled++;
            }
        }

        if (sampled > 0)
        {
            reasons.Add($"{sampled} unit(s) sampled as a canary");
        }
    }
}
