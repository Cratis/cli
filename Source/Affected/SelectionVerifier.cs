// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Checks that what a runner says a filter matches is exactly what was selected.
/// </summary>
public static class SelectionVerifier
{
    /// <summary>
    /// Compares the selected units with the units the filter matched.
    /// </summary>
    /// <param name="expected">The units the selection means to run.</param>
    /// <param name="matched">The units the runner listed for the generated filter.</param>
    /// <returns>Null when they are equal, otherwise a description of the difference.</returns>
    public static string? Compare(IReadOnlySet<string> expected, IReadOnlySet<string> matched)
    {
        var missing = expected.Where(unit => !matched.Contains(unit)).Order(StringComparer.Ordinal).ToList();
        var extra = matched.Where(unit => !expected.Contains(unit)).Order(StringComparer.Ordinal).ToList();
        if (missing.Count == 0 && extra.Count == 0)
        {
            return null;
        }

        return $"the generated filter does not match the selection ({missing.Count} selected unit(s) not matched, e.g. {string.Join(", ", missing.Take(3))}; {extra.Count} unselected unit(s) matched, e.g. {string.Join(", ", extra.Take(3))})";
    }
}
