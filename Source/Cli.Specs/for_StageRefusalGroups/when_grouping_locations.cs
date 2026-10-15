// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageRefusalGroups;

public class when_grouping_locations : Specification
{
    StageRefusalGroup _group;

    void Because() => _group = StageRefusalGroups.Create([
        new("Plan", "model", "SourceLocation { Line = 13, Column = 5, Path = first.play }: STAGE-ESM-034: Command 'First' reads 'One' with legacy semantics."),
        new("Plan", "model", "second.play(14,5): STAGE-ESM-034: Command 'Second' reads 'Two' with legacy semantics."),
        new("Plan", "Third", "STAGE-ESM-034: Command 'Third' reads 'Three' with legacy semantics."),
        new("Plan", "model", "fourth.play(16,5): STAGE-ESM-034: Command 'Fourth' reads 'Four' with legacy semantics."),
        new("Plan", "model", "fifth.play(17,5): STAGE-ESM-034: Command 'Fifth' reads 'Five' with legacy semantics.")
    ]).Single();

    [Fact] void should_count_every_original_issue() => _group.Count.ShouldEqual(5);
    [Fact] void should_normalize_names_without_losing_the_stage_code() => _group.Kind.ShouldEqual("STAGE-ESM-034: Command '…' reads '…' with legacy semantics.");
    [Fact] void should_keep_the_first_three_locations_including_an_artifact_fallback() => _group.Locations.ShouldEqual("SourceLocation { Line = 13, Column = 5, Path = first.play }", "second.play(14,5)", "Third");
    [Fact] void should_show_how_many_other_locations_exist() => _group.Summary.ShouldEqual("5 × STAGE-ESM-034: Command '…' reads '…' with legacy semantics. at SourceLocation { Line = 13, Column = 5, Path = first.play }, second.play(14,5), Third (+2 more)");
}
