// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating.and_generation_reports_an_error;

[Collection(CliSpecsCollection.Name)]
public class and_no_source_was_generated : given.a_generation_reporting_an_error
{
    const string ExistingSource = "domain Existing\n";

    string _outputPath;
    int _result;

    void Establish()
    {
        _outputPath = Path.Combine(_folder, "MyApp.play");
        File.WriteAllText(_outputPath, ExistingSource);
        _settings.File = "MyApp.play";
        _settings.FeatureRoot = "../Features";
        _generation
            .Generate(Arg.Any<string>(), Arg.Any<ScreenplayGenerationOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new GeneratedScreenplay(
                string.Empty,
                [new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "DOTNETSP0002", "The project-relative feature root is invalid", "MyApp.slnx")])));
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_fail_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_preserve_the_existing_file() => File.ReadAllBytes(_outputPath).ShouldEqual(Encoding.UTF8.GetBytes(ExistingSource));
    [Fact] void should_not_write_a_document_to_standard_output() => _standardOutput.ToArray().ShouldBeEmpty();
}
