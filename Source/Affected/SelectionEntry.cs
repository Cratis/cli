// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// One item of a filtered selection: a single unit, or everything under a namespace.
/// </summary>
/// <param name="Value">The unit name or the namespace.</param>
/// <param name="IsNamespace">Whether it stands for every unit under the namespace.</param>
public record SelectionEntry(string Value, bool IsNamespace)
{
    /// <summary>
    /// Gets whether a unit falls under this entry.
    /// </summary>
    /// <param name="unit">The unit name.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <returns>True when the entry selects the unit.</returns>
    public bool Selects(string unit, UnitGranularity granularity)
    {
        if (!IsNamespace)
        {
            return unit == Value;
        }

        var ns = UnitNames.NamespaceOf(unit, granularity);
        return ns == Value || ns.StartsWith(Value + ".", StringComparison.Ordinal);
    }
}
