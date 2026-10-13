// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_event_id_publication_is_refused_in_json : given.an_event_id_refusal
{
    (int ExitCode, string Stdout, string Stderr) _output;

    string _message = null!;

    async Task Because()
    {
        _output = await Capture();
        using var diagnostic = JsonDocument.Parse(_output.Stderr.Split('\n')[0]);
        _message = diagnostic.RootElement.GetProperty("diagnostics")[0].GetProperty("message").GetString()!;
    }

    [Fact] void should_return_validation_error() => _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_surface_the_cli_code() => _output.Stderr.ShouldContain("CLI-RENDER-016");
    [Fact] void should_surface_the_stage_diagnostic() => _output.Stderr.ShouldContain("STAGE-EVENT-002: Event type");
    [Fact] void should_report_the_pin_remedy() => _message.ShouldContain("Pin the event's 'id'");
    [Fact] void should_report_no_success_on_stdout() => _output.Stdout.ShouldBeEmpty();
}
