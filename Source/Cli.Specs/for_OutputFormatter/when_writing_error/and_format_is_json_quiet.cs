// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_OutputFormatter.when_writing_error;

[Collection(CliSpecsCollection.Name)]
public class and_format_is_json_quiet : Specification
{
    string _output;

    void Because()
    {
        var writer = new StringWriter();
        Console.SetError(writer);

        OutputFormatter.WriteError(
            OutputFormats.JsonQuiet,
            "Confirmation is required",
            "Re-run with --yes",
            ExitCodes.ValidationErrorCode);

        _output = writer.ToString();
        Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
    }

    [Fact] void should_contain_the_error_code() => _output.ShouldContain("\"error\":\"validation_error\"");
    [Fact] void should_contain_the_yes_hint() => _output.ShouldContain("--yes");
    [Fact] void should_be_valid_json() => System.Text.Json.JsonDocument.Parse(_output).ShouldNotBeNull();
}
