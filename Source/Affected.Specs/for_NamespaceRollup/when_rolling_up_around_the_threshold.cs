// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.for_NamespaceRollup;

public class when_rolling_up_around_the_threshold : Specification
{
    IReadOnlyList<string> _units;
    IReadOnlyList<SelectionEntry> _atThreshold;
    IReadOnlyList<SelectionEntry> _belowThreshold;
    IReadOnlyList<SelectionEntry> _tooFewUnits;

    void Because()
    {
        // Ten units in Root.Big and thirty in Root.Other, so a rolled-up Root.Big stays well under half of the project.
        _units = [.. Enumerable.Range(0, 10).Select(i => $"Root.Big.C{i}"), .. Enumerable.Range(0, 30).Select(i => $"Root.Other.C{i}")];
        _atThreshold = NamespaceRollup.Apply(_units, Pick("Root.Big", 5), UnitGranularity.Class, 0.5, 5)!;
        _belowThreshold = NamespaceRollup.Apply(_units, Pick("Root.Big", 4), UnitGranularity.Class, 0.5, 5)!;
        _tooFewUnits = NamespaceRollup.Apply(_units, Pick("Root.Big", 5), UnitGranularity.Class, 0.5, 11)!;
    }

    [Fact] void should_replace_the_units_with_the_namespace_at_the_threshold() => _atThreshold.ShouldContainOnly(new SelectionEntry("Root.Big", true));
    [Fact] void should_keep_individual_units_below_the_threshold() => _belowThreshold.Count.ShouldEqual(4);
    [Fact] void should_not_roll_up_below_the_threshold() => _belowThreshold.ShouldEachConformTo(entry => !entry.IsNamespace);
    [Fact] void should_not_roll_up_a_namespace_with_too_few_units() => _tooFewUnits.ShouldEachConformTo(entry => !entry.IsNamespace);

    static HashSet<string> Pick(string ns, int count) => [.. Enumerable.Range(0, count).Select(i => $"{ns}.C{i}")];
}
