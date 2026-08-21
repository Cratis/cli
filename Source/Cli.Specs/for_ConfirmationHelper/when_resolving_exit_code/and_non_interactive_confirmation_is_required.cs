// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConfirmationHelper.when_resolving_exit_code;

[Collection(CliSpecsCollection.Name)]
public class and_non_interactive_confirmation_is_required : Specification
{
    TextWriter _previousError;
    string _output;
    int? _result;

    void Because()
    {
        _previousError = Console.Error;
        var writer = new StringWriter();
        Console.SetError(writer);

        _result = ConfirmationHelper.ExitCodeFor(ConfirmationOutcome.ConfirmationRequired, OutputFormats.JsonCompact);

        _output = writer.ToString();
        Console.SetError(_previousError);
    }

    [Fact] void should_return_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_write_the_validation_error_to_stderr() => _output.ShouldContain("\"error\":\"validation_error\"");
    [Fact] void should_include_the_yes_hint() => _output.ShouldContain("--yes");
    [Fact] void should_write_valid_json() => System.Text.Json.JsonDocument.Parse(_output).ShouldNotBeNull();
}
