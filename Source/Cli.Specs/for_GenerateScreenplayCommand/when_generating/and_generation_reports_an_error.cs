// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating;

[Collection(CliSpecsCollection.Name)]
public class and_generation_reports_an_error : given.a_generate_screenplay_command
{
    int _result;

    void Establish()
    {
        _settings.File = "MyApp.play";
        _generation
            .Generate(Arg.Any<string>(), Arg.Any<ScreenplayGenerationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GeneratedScreenplay(
                GeneratedSource,
                [
                    new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Warning, "SP0100", "a warning", null),
                    new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "SP0200", "an error", "Library.Lending")
                ])));
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_fail_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_write_the_document_to_the_file() => File.Exists(Path.Combine(_folder, "MyApp.play")).ShouldBeFalse();
    [Fact] void should_not_write_the_document_to_standard_output() => _standardOutput.ToArray().ShouldBeEmpty();
}
