// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_bundled_strict_acceptance : given.a_render_acceptance_command
{
    (int ExitCode, string Stdout, string Stderr) _output;

    void Establish()
    {
        _settings.Strict = true;
        _command = new RenderCommand(_planning, _publication);
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_accept_a_plan_without_degradation_evidence() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_treat_an_unrelated_warning_as_degradation() => _output.Stderr.ShouldNotContain(ExitCodes.StrictUnacceptedCode);
    [Fact] void should_retain_safe_artifacts() => File.Exists(_artifactPath).ShouldBeTrue();
    [Fact] void should_retain_the_mock_host_report() => File.Exists(_reportPath).ShouldBeTrue();
}
