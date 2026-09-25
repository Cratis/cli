// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Cli.for_ScreenplayPlanning.when_rendering_implementation_attachments;

public class with_a_missing_file : given.a_model_root
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => WriteSource();
    async Task Because() => _result = await _planning.Plan(new(_root, "Orders", "cratis"), CancellationToken.None);

    [Fact] void should_warn_about_the_missing_file() => _result.Diagnostics.Any(_ => _.Code == "PLAY0432" && _.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_pass_the_requirement_to_stage() => _renderRequest!.ImplementationRequirements.Length.ShouldEqual(1);
    [Fact] void should_block_the_missing_body_with_its_reason() => _result.Diagnostics.Any(_ => _.Code == "STAGE-ESM-020" && _.Message.Contains("PLAY0432", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_publish_artifacts() => _result.Artifacts!.Artifacts.ShouldBeEmpty();
}
