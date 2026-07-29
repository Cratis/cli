// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating.and_generation_reports_an_error;

[Collection(CliSpecsCollection.Name)]
public class and_a_file_is_given : given.a_generation_reporting_an_error
{
    string _outputPath;
    int _result;

    void Establish()
    {
        _outputPath = Path.Combine(_folder, "MyApp.play");
        _settings.File = "MyApp.play";
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_fail_with_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_still_write_the_document_to_the_file() => File.ReadAllBytes(_outputPath).ShouldEqual(Encoding.UTF8.GetBytes(GeneratedSource));
    [Fact] void should_not_write_the_document_to_standard_output() => _standardOutput.ToArray().ShouldBeEmpty();
}
