// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_StageReadiness;

public class when_reading_refusals : Specification
{
    StageRuntimeStatus _status;

    void Because() => _status = StageRuntimeStatus.Read(
        """{"state":"unsupported","model":null,"handoffId":null,"engine":"eventmodel","issues":[""" +
        """{"capability":"Routing","artifact":"Register","details":"STAGE-ESM-030: Routes run only on the semantic engine."},""" +
        """{"capability":"Capture","artifact":"Import","details":"STAGE-ESM-033: Foreign events are not supported."}]}""")!;

    [Fact] void should_preserve_every_issue() => _status.Issues.ShouldContainOnly([new StageRuntimeIssue("Routing", "Register", "STAGE-ESM-030: Routes run only on the semantic engine."), new StageRuntimeIssue("Capture", "Import", "STAGE-ESM-033: Foreign events are not supported.")]);
    [Fact] void should_suggest_the_semantic_engine_for_eventmodel_routes() => _status.SuggestionFor(new() { Engine = "eventmodel" }).ShouldContain("--engine semantic");
    [Fact] void should_not_suggest_switching_a_semantic_refusal_to_itself() => (_status with { Engine = "semantic" }).SuggestionFor(new() { Engine = "semantic" }).ShouldNotContain("--engine semantic");
    [Fact] void should_not_suggest_semantic_for_unrelated_capability_gaps() => (_status with { Issues = [_status.Issues![1]] }).SuggestionFor(new() { Engine = "eventmodel" }).ShouldNotContain("--engine semantic");
}
