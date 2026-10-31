// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_strict_degraded_acceptance : given.a_render_acceptance_command
{
    (int ExitCode, string Stdout, string Stderr) _output;
    bool _outputsExistedAtAcceptance;

    void Establish()
    {
        _settings.Strict = true;
        _acceptance(Arg.Any<ScreenplayRenderPlan>(), true).Returns(_ =>
        {
            _outputsExistedAtAcceptance = File.Exists(_artifactPath) && File.Exists(_reportPath);
            return AcceptanceFailure;
        });
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_fail_acceptance() => _output.ExitCode.ShouldEqual(ExitCodes.StrictUnaccepted);
    [Fact] void should_have_published_artifacts_and_reports_before_acceptance() => _outputsExistedAtAcceptance.ShouldBeTrue();
    [Fact] void should_retain_allowed_artifacts() => File.ReadAllText(_artifactPath).ShouldEqual("allowed artifact");
    [Fact] void should_retain_the_mock_host_report() => File.ReadAllText(_reportPath).ShouldEqual("mock degradation report");
    [Fact] void should_emit_the_publication_receipt() => _output.Stdout.ShouldContain("publication");
    [Fact] void should_report_acceptance_failure() => _output.Stderr.ShouldContain(AcceptanceFailure);
    [Fact] void should_emit_the_strict_unaccepted_machine_code() => _output.Stderr.ShouldContain(ExitCodes.StrictUnacceptedCode);
    [Fact]
    void should_forward_strict_acceptance_after_publication() => Received.InOrder(() =>
    {
        _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
        _acceptance(Arg.Any<ScreenplayRenderPlan>(), true);
    });
}
