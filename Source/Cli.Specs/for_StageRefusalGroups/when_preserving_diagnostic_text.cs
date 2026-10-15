// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageRefusalGroups;

public class when_preserving_diagnostic_text : Specification
{
    StageRefusalGroup _group;

    void Because() => _group = StageRefusalGroups.Create([new("Plan", "Limit", "STAGE-ESM-038: Rule 'Limit(1,2)' cannot run.")]).Single();

    [Fact] void should_not_treat_parentheses_in_the_message_as_a_source_prefix() => _group.Kind.ShouldEqual("STAGE-ESM-038: Rule '…' cannot run.");
    [Fact] void should_use_the_artifact_as_the_location() => _group.Locations.ShouldEqual("Limit");
}
