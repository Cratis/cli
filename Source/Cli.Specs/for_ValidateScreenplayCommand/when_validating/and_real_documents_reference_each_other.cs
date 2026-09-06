// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_validating;

[Collection(CliSpecsCollection.Name)]
public class and_real_documents_reference_each_other : for_ScreenplayValidation.given.a_folder_with_documents
{
    TextWriter _previousOutput;
    TextWriter _previousError;
    StringWriter _output;
    StringWriter _error;
    JsonDocument _summary;
    int _result;

    void Establish()
    {
        WriteDocument("Commands.play", CommandSource);
        WriteDocument("nested/Events.play", EventSource);
        WriteDocument("Concepts.play", ConceptsSource);
        _previousOutput = Console.Out;
        _previousError = Console.Error;
        _output = new StringWriter();
        _error = new StringWriter();
        Console.SetOut(_output);
        Console.SetError(_error);
    }

    async Task Because()
    {
        _result = await ((ICommand<ValidateScreenplaySettings>)new ValidateScreenplayCommand()).ExecuteAsync(
            new CommandContext([], Substitute.For<IRemainingArguments>(), "validate", null),
            new ValidateScreenplaySettings { Path = _folder, Output = OutputFormats.JsonCompact },
            CancellationToken.None);
        _summary = JsonDocument.Parse(_output.ToString());
    }

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_the_discovered_file_count() => _summary.RootElement.GetProperty("files").GetInt32().ShouldEqual(3);
    [Fact] void should_report_no_diagnostics() => _summary.RootElement.GetProperty("diagnostics").GetInt32().ShouldEqual(0);
    [Fact] void should_not_write_unresolved_reference_warnings() => _error.ToString().ShouldBeEmpty();

    void Destroy()
    {
        Console.SetOut(_previousOutput);
        Console.SetError(_previousError);
        _output.Dispose();
        _error.Dispose();
        _summary?.Dispose();
    }
}
