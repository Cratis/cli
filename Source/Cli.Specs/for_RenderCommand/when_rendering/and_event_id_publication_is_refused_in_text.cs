// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_event_id_publication_is_refused_in_text : given.an_event_id_refusal
{
    (int ExitCode, string Stdout, string Stderr) _output;

    void Establish() => _settings.Output = OutputFormats.Table;
    async Task Because() => _output = await Capture();

    [Fact] void should_return_validation_error() => _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_surface_the_cli_code() => _output.Stderr.ShouldContain("CLI-RENDER-016");
    [Fact] void should_surface_the_stage_diagnostic() => _output.Stderr.ShouldContain("STAGE-EVENT-002: Event type");
    [Fact] void should_report_the_pin_remedy() => _output.Stderr.ShouldContain("Pin the event's 'id'");
}
