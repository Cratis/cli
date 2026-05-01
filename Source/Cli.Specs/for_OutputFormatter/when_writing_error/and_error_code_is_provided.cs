// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_OutputFormatter.when_writing_error;

[Collection(CliSpecsCollection.Name)]
public class and_error_code_is_provided : Specification
{
    string _output;

    void Because()
    {
        var writer = new StringWriter();
        Console.SetError(writer);

        OutputFormatter.WriteError(OutputFormats.Json, "Resource not found", "Check the identifier", ExitCodes.NotFoundCode);

        _output = writer.ToString();
        Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
    }

    [Fact] void should_contain_error_code() => _output.ShouldContain("\"error\": \"not_found\"");
    [Fact] void should_contain_message() => _output.ShouldContain("\"message\": \"Resource not found\"");
    [Fact] void should_contain_suggestion() => _output.ShouldContain("\"suggestion\": \"Check the identifier\"");
    [Fact] void should_be_valid_json() => System.Text.Json.JsonDocument.Parse(_output).ShouldNotBeNull();
}
