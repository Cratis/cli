// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_an_unsafe_plan_and_strict_acceptance : given.a_render_acceptance_command
{
    (int ExitCode, string Stdout, string Stderr) _output;

    void Establish()
    {
        _settings.Strict = true;
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ScreenplayRenderPlan(
                1,
                [new(ScreenplayDiagnosticSeverity.Error, "MOCK-UNSAFE", "Unsafe generation", null)],
                _artifactPlan));
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_refuse_generation() => _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_real_safety_error() => _output.Stderr.ShouldContain("MOCK-UNSAFE");
    [Fact] void should_not_publish() => _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_consult_acceptance() => _acceptance.DidNotReceive()(Arg.Any<ScreenplayRenderPlan>(), Arg.Any<bool>());
    [Fact] void should_not_create_the_destination() => Directory.Exists(_destination).ShouldBeFalse();
    [Fact] void should_not_emit_a_publication_receipt() => _output.Stdout.ShouldEqual(string.Empty);
}
