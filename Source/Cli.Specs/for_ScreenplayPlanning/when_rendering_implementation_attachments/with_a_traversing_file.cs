// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;

namespace Cratis.Cli.for_ScreenplayPlanning.when_rendering_implementation_attachments;

public class with_a_traversing_file : given.a_model_root
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => WriteSource(FileSource.Replace("Rules/Positive.cs", "../outside.cs", StringComparison.Ordinal));
    async Task Because() => _result = await _planning.Plan(new(_root, "Orders", "cratis"), CancellationToken.None);

    [Fact] void should_warn_about_the_refused_file() => _result.Diagnostics.Any(_ => _.Code == "PLAY0430" && _.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_block_the_body_with_the_refusal_reason() => _result.Diagnostics.Any(_ => _.Code == "STAGE-ESM-020" && _.Message.Contains("PLAY0430", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_publish_artifacts() => _result.Artifacts!.Artifacts.ShouldBeEmpty();
}
