// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_rendering_implementation_attachments;

public class with_inline_policy : given.a_model_root
{
    ScreenplayRenderPlan _result = null!;
    void Establish() => WriteSource(PolicySource);
    async Task Because() => _result = await _planning.Plan(new(_path, "Orders", "cratis"), CancellationToken.None);

    [Fact] void should_report_the_parser_rejection_before_stage_blocks_publication() => _result.Diagnostics.Select(_ => _.Code).ShouldContain("PLAY0478");
    [Fact] void should_report_the_stage_publication_blocker() => _result.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-006");
    [Fact] void should_not_report_a_missing_body() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain("STAGE-ESM-020");
    [Fact] void should_not_publish_artifacts() => _result.Artifacts!.Artifacts.ShouldBeEmpty();
}
