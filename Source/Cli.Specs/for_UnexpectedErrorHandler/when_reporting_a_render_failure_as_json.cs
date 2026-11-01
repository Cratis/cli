// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_UnexpectedErrorHandler;

public class when_reporting_a_render_failure_as_json : given.an_error_handler
{
    JsonElement[] _documents = null!;

    void Because()
    {
        _exitCode = UnexpectedErrorHandler.Handle(new RenderFailedUnexpectedly("planning", _failure), ["render", "-o", "json"], _console);
        var stream = _output.ToString();
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(stream), new JsonReaderOptions { AllowMultipleValues = true });
        var documents = new List<JsonElement>();
        while (reader.Read())
        {
            using var document = JsonDocument.ParseValue(ref reader);
            documents.Add(document.RootElement.Clone());
        }
        _documents = [.. documents];
    }

    [Fact] void should_return_unexpected_error() => _exitCode.ShouldEqual(10);
    [Fact] void should_report_the_machine_error_code() => _documents.ShouldContain(document => document.TryGetProperty("error", out var code) && code.GetString() == "unexpected_error");
    [Fact] void should_report_the_diagnostic_code() => _output.ToString().ShouldContain("CLI-RENDER-027");
    [Fact] void should_not_print_stack_frames() => _output.ToString().ShouldNotContain("   at ");
}
