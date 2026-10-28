// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_default_degraded_acceptance : given.a_render_acceptance_command
{
    (int ExitCode, string Stdout, string Stderr) _output;

    async Task Because() => _output = await Capture();

    [Fact] void should_succeed() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_publish_allowed_artifacts() => File.ReadAllText(_artifactPath).ShouldEqual("allowed artifact");
    [Fact] void should_publish_the_mock_host_report() => File.ReadAllText(_reportPath).ShouldEqual("mock degradation report");
    [Fact] void should_forward_default_acceptance() => _acceptance.Received(1)(Arg.Any<ScreenplayRenderPlan>(), false);
    [Fact] void should_report_the_warning_without_promoting_it() => _output.Stderr.ShouldContain("MOCK-DEGRADED");
    [Fact] void should_not_report_acceptance_failure() => _output.Stderr.ShouldNotContain(AcceptanceFailure);
}
