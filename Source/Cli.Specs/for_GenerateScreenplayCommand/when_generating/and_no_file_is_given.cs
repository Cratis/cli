// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating;

[Collection(CliSpecsCollection.Name)]
public class and_no_file_is_given : given.a_generate_screenplay_command
{
    int _result;

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_write_the_document_to_standard_output() => _standardOutput.ToArray().ShouldEqual(Encoding.UTF8.GetBytes(GeneratedSource));
    [Fact] void should_generate_from_the_discovered_solution() => _generation.Received(1).Generate(_solution, Arg.Any<ScreenplayGenerationOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_write_a_file() => Directory.GetFiles(_folder, "*.play").ShouldBeEmpty();
}
