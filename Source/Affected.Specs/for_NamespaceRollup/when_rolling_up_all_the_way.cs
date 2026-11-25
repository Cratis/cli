// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.for_NamespaceRollup;

public class when_rolling_up_all_the_way : Specification
{
    IReadOnlyList<SelectionEntry>? _result;
    IReadOnlyList<SelectionEntry>? _nested;

    void Because()
    {
        var units = Enumerable.Range(0, 10).Select(i => $"Root.A.C{i}").ToList();
        _result = NamespaceRollup.Apply(units, new HashSet<string>(units.Take(6)), UnitGranularity.Class, 0.5, 5);

        var deep = Enumerable.Range(0, 6).Select(i => $"Root.A.B.C{i}")
            .Concat(Enumerable.Range(0, 6).Select(i => $"Root.A.D.C{i}"))
            .Concat(Enumerable.Range(0, 30).Select(i => $"Root.Z.C{i}"))
            .ToList();
        _nested = NamespaceRollup.Apply(deep, new HashSet<string>(deep.Where(unit => unit.StartsWith("Root.A.B", StringComparison.Ordinal) || unit.StartsWith("Root.A.D.C0", StringComparison.Ordinal))), UnitGranularity.Class, 0.5, 5);
    }

    [Fact] void should_report_the_project_root_as_everything() => _result.ShouldBeNull();
    [Fact] void should_continue_upward_through_nested_namespaces() => _nested.ShouldContainOnly(new SelectionEntry("Root.A", true));
}
