// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating;

[Collection(CliSpecsCollection.Name)]
public class and_a_file_is_given : given.a_generate_screenplay_command
{
    string _outputPath;
    int _result;

    void Establish()
    {
        _outputPath = Path.Combine(_folder, "plays", "MyApp.play");
        _settings.File = Path.Combine("plays", "MyApp.play");
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_write_the_document_to_the_file() => File.ReadAllBytes(_outputPath).ShouldEqual(Encoding.UTF8.GetBytes(GeneratedSource));
    [Fact] void should_create_the_folder_the_file_lives_in() => Directory.Exists(Path.GetDirectoryName(_outputPath)).ShouldBeTrue();
    [Fact] void should_not_write_the_document_to_standard_output() => _standardOutput.ToArray().ShouldBeEmpty();
}
