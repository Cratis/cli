// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_baseline_warnings_are_reported_in_text : given.a_baseline_workspace
{
    void Establish() => _settings.Output = OutputFormats.Table;
    async Task Because() => _output = await Capture();

    [Fact] void should_return_success() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_the_stage_warning_on_stderr() => _output.Stderr.ShouldContain("STAGE-BASELINE-001");
    [Fact] void should_name_the_admitted_release() => _output.Stderr.ShouldContain("v1.0");
    [Fact] void should_keep_warnings_out_of_stdout() => _output.Stdout.ShouldNotContain("STAGE-BASELINE-");
}
