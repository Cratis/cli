// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Expresses selections as xUnit v3 query filters. Repeated -filter arguments are ORed by the runner, each query matches
/// namespace and class exactly, and a class never matches a sibling that merely shares its name as a prefix.
/// </summary>
public static class XunitV3Filter
{
    /// <summary>
    /// Creates the query that selects one unit.
    /// </summary>
    /// <param name="unit">The unit name.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <returns>The query.</returns>
    public static IReadOnlyList<string> QueriesForUnit(string unit, UnitGranularity granularity)
    {
        if (granularity == UnitGranularity.Test)
        {
            var type = UnitNames.ClassOf(unit);
            return [$"/*/{UnitNames.NamespaceOf(unit, granularity)}/{TypeName(type)}/{unit[(type.Length + 1)..]}"];
        }

        return [$"/*/{UnitNames.NamespaceOf(unit, granularity)}/{TypeName(unit)}/*"];
    }

    /// <summary>
    /// Creates the queries that select everything under a namespace, including nested namespaces.
    /// </summary>
    /// <param name="ns">The namespace.</param>
    /// <returns>The queries.</returns>
    public static IReadOnlyList<string> QueriesForNamespace(string ns) => [$"/*/{ns}/*/*", $"/*/{ns}.*/*/*"];

    /// <summary>
    /// Creates the arguments for a selection, split so no run's arguments exceed a length.
    /// </summary>
    /// <param name="entries">The selection.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <param name="maxLength">The longest argument text of one run.</param>
    /// <returns>One argument list per run.</returns>
    public static IReadOnlyList<IReadOnlyList<string>> CreateBatches(IReadOnlyList<SelectionEntry> entries, UnitGranularity granularity, int maxLength)
    {
        var batches = new List<IReadOnlyList<string>>();
        var current = new List<string>();
        var length = 0;
        foreach (var query in entries.SelectMany(entry => entry.IsNamespace ? QueriesForNamespace(entry.Value) : QueriesForUnit(entry.Value, granularity)))
        {
            var cost = query.Length + 10;
            if (current.Count > 0 && length + cost > maxLength)
            {
                batches.Add(current);
                current = [];
                length = 0;
            }

            current.Add("-filter");
            current.Add(query);
            length += cost;
        }

        if (current.Count > 0)
        {
            batches.Add(current);
        }

        return batches;
    }

    static string TypeName(string fullName)
    {
        var index = fullName.LastIndexOf('.');
        return index < 0 ? fullName : fullName[(index + 1)..];
    }
}
