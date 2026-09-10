// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating;

[Collection(CliSpecsCollection.Name)]
public class and_the_default_file_already_exists : given.a_generate_screenplay_command
{
    const string PreviousContent = "domain PreviousRun\n\nmodule PreviousRun\n";

    int _result;

    void Establish() => File.WriteAllText(DefaultOutputPath, PreviousContent);

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_overwrite_the_existing_file() => File.ReadAllText(DefaultOutputPath).ShouldEqual(PreviousContent);
    [Fact] void should_write_the_document_to_an_incremented_file() => File.ReadAllBytes(Path.Combine(_folder, "Screenplay-1.play")).ShouldEqual(Encoding.UTF8.GetBytes(GeneratedSource));
}
