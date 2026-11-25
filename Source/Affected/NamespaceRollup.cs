// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Replaces many selected units with a single entry for their namespace when most of the namespace is selected anyway.
/// This keeps filters short and cheap to evaluate, at the price of running a few extra tests.
/// </summary>
public static class NamespaceRollup
{
    /// <summary>
    /// Rolls a selection up.
    /// </summary>
    /// <param name="allUnits">Every unit of the project.</param>
    /// <param name="selected">The selected units.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <param name="threshold">The selected fraction at which a namespace is rolled up.</param>
    /// <param name="minimumUnits">The number of units a namespace needs to be rolled up.</param>
    /// <returns>The entries, or null when the roll-up reached the project root, which means everything runs.</returns>
    public static IReadOnlyList<SelectionEntry>? Apply(IReadOnlyCollection<string> allUnits, IReadOnlySet<string> selected, UnitGranularity granularity, double threshold, int minimumUnits)
    {
        var namespaceOf = allUnits.ToDictionary(unit => unit, unit => UnitNames.NamespaceOf(unit, granularity), StringComparer.Ordinal);
        var effective = new HashSet<string>(selected, StringComparer.Ordinal);
        var rolled = new List<string>();

        var namespaces = namespaceOf.Values.SelectMany(Prefixes).Append(string.Empty).Distinct(StringComparer.Ordinal)
            .OrderByDescending(Depth)
            .ThenBy(ns => ns, StringComparer.Ordinal);

        foreach (var ns in namespaces)
        {
            var under = allUnits.Where(unit => IsUnder(namespaceOf[unit], ns)).ToList();
            var count = under.Count(effective.Contains);
            if (under.Count < minimumUnits || count == 0 || (double)count / under.Count < threshold)
            {
                continue;
            }

            effective.UnionWith(under);
            rolled.Add(ns);
        }

        if (rolled.Contains(string.Empty))
        {
            return null;
        }

        var topLevel = rolled.Where(ns => !rolled.Exists(other => other != ns && IsUnder(ns, other))).Order(StringComparer.Ordinal).ToList();
        var singles = selected.Where(unit => !topLevel.Exists(ns => IsUnder(namespaceOf[unit], ns))).Order(StringComparer.Ordinal);
        return [.. topLevel.Select(ns => new SelectionEntry(ns, true)), .. singles.Select(unit => new SelectionEntry(unit, false))];
    }

    static bool IsUnder(string candidate, string ns) =>
        ns.Length == 0 || candidate == ns || candidate.StartsWith(ns + ".", StringComparison.Ordinal);

    static int Depth(string ns) => ns.Length == 0 ? 0 : ns.Count(c => c == '.') + 1;

    static IEnumerable<string> Prefixes(string ns)
    {
        for (var index = ns.IndexOf('.', StringComparison.Ordinal); index >= 0; index = ns.IndexOf('.', index + 1))
        {
            yield return ns[..index];
        }

        if (ns.Length > 0)
        {
            yield return ns;
        }
    }
}
