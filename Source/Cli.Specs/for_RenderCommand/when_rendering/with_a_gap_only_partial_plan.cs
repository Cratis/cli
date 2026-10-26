// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_a_gap_only_partial_plan : given.a_partial_render_acceptance_command
{
    (int ExitCode, string Stdout, string Stderr) _output;

    async Task Because() => _output = await Capture();

    [Fact] void should_have_a_publishable_gap_only_plan() => _artifactPlan.Success.ShouldBeTrue();
    [Fact] void should_exercise_partial_not_refused() => _artifactPlan.Outcome.ShouldEqual(ArtifactRenderOutcome.Partial);
    [Fact] void should_have_an_implementation_gap() => _artifactPlan.ImplementationGaps.Length.ShouldEqual(1);
    [Fact] void should_publish_without_strict_failure() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_publish_allowed_artifacts() => File.Exists(_artifactPath).ShouldBeTrue();
    [Fact] void should_emit_a_publication_receipt() => _output.Stdout.ShouldContain("publication");
    [Fact] void should_not_report_acceptance_failure() => _output.Stderr.ShouldEqual(string.Empty);
}
