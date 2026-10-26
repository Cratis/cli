// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_accepted_strict_rendering : given.a_render_acceptance_command
{
    (int ExitCode, string Stdout, string Stderr) _output;

    void Establish()
    {
        _settings.Strict = true;
        _acceptance(Arg.Any<ScreenplayRenderPlan>(), true).Returns((string?)null);
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_succeed() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_publish_allowed_artifacts() => File.Exists(_artifactPath).ShouldBeTrue();
    [Fact] void should_publish_the_mock_report() => File.Exists(_reportPath).ShouldBeTrue();
    [Fact] void should_not_report_strict_rejection() => _output.Stderr.ShouldNotContain(ExitCodes.StrictUnacceptedCode);
}
