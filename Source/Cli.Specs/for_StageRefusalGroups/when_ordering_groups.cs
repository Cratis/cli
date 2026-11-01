// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageRefusalGroups;

public class when_ordering_groups : Specification
{
    IReadOnlyList<StageRefusalGroup> _groups;

    void Because() => _groups = StageRefusalGroups.Create([
        new("Plan", "Metadata", "Persona 'One' is authoring metadata."),
        new("Plan", "Z", "Z is unsupported."),
        new("Plan", "A", "A is unsupported."),
        new("Plan", "B", "B is unsupported."),
        new("Plan", "B", "B is unsupported."),
        new("Plan", "Specification", "Specification 'given' is missing required property 'id'."),
        new("Plan", "Metadata", "Persona 'Two' is authoring metadata.")
    ]);

    [Fact] void should_order_categories_by_the_users_next_action() => _groups.Select(group => group.Category).Distinct().ShouldEqual("Fix your model", "Not supported by this engine yet", "Ignored by this engine");
    [Fact] void should_order_kinds_by_count_then_ordinal_text() => _groups.Where(group => group.Category == StageRefusalGroups.NotSupported).Select(group => group.Kind).ShouldEqual("B is unsupported.", "A is unsupported.", "Z is unsupported.");
    [Fact] void should_not_report_more_locations_for_small_groups() => _groups[0].Summary.ShouldNotContain("more)");
}
