// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageRefusalGroups;

public class when_grouping_notes : Specification
{
    IReadOnlyList<StageRefusalGroup> _groups = null!;

    void Because() => _groups = StageRefusalGroups.Create(
        [new("Plan", "BadConcept", "STAGE-ESM-005: Concept 'Bad' cannot render.")],
        [
            new("PLAY0270", "Concept 'One' declares a report-only annotation.", "one.play(1,1)"),
            new("PLAY0270", "Concept 'Two' declares a report-only annotation.", "two.play(2,1)"),
            new("PLAY0270", "Concept 'Three' declares a report-only annotation.", "three.play(3,1)"),
            new("PLAY0270", "Concept 'Four' declares a report-only annotation.", "four.play(4,1)"),
            new("PLAY0999", "Concept 'Five' declares a report-only annotation.", "five.play(5,1)")
        ]);

    [Fact] void should_keep_a_blocking_issue_out_of_ignored_without_metadata_wording() => _groups[0].Category.ShouldEqual(StageRefusalGroups.NotSupported);
    [Fact] void should_always_categorize_notes_as_ignored() => _groups.Skip(1).ShouldEachConformTo(group => group.Category == StageRefusalGroups.Ignored);
    [Fact] void should_normalize_names_and_keep_the_code() => _groups[1].Kind.ShouldEqual("PLAY0270: Concept '…' declares a report-only annotation.");
    [Fact] void should_group_the_four_matching_notes() => _groups[1].Count.ShouldEqual(4);
    [Fact] void should_take_the_first_three_note_sources() => _groups[1].Locations.ShouldEqual("one.play(1,1)", "two.play(2,1)", "three.play(3,1)");
    [Fact] void should_report_the_remaining_locations() => _groups[1].Summary.ShouldContain("(+1 more)");
    [Fact] void should_keep_different_codes_separate() => _groups[2].Kind.ShouldContain("PLAY0999:");
}
