// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected;

/// <summary>
/// Helpers for the names of test units.
/// </summary>
public static class UnitNames
{
    /// <summary>
    /// Gets the namespace a unit lives in.
    /// </summary>
    /// <param name="unit">The unit name.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <returns>The namespace, empty for the global namespace.</returns>
    public static string NamespaceOf(string unit, UnitGranularity granularity)
    {
        var typeName = granularity == UnitGranularity.Test ? ClassOf(unit) : unit;
        var index = typeName.LastIndexOf('.');
        return index < 0 ? string.Empty : typeName[..index];
    }

    /// <summary>
    /// Gets the fully qualified name of the class a unit belongs to.
    /// </summary>
    /// <param name="unit">A class or test name.</param>
    /// <returns>The class name.</returns>
    public static string ClassOf(string unit)
    {
        var index = unit.LastIndexOf('.');
        return index < 0 ? unit : unit[..index];
    }

    /// <summary>
    /// Gets the namespace group a unit belongs to: its namespace cut after the first segment that starts with for_,
    /// or the whole namespace when there is none.
    /// </summary>
    /// <param name="unit">The unit name.</param>
    /// <param name="granularity">What a unit is.</param>
    /// <returns>The group name.</returns>
    public static string GroupOf(string unit, UnitGranularity granularity)
    {
        var segments = NamespaceOf(unit, granularity).Split('.');
        var index = Array.FindIndex(segments, segment => segment.StartsWith("for_", StringComparison.Ordinal));
        return string.Join('.', index < 0 ? segments : segments[..(index + 1)]);
    }
}
