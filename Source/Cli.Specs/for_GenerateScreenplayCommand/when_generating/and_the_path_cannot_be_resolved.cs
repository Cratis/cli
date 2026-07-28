// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating;

[Collection(CliSpecsCollection.Name)]
public class and_the_path_cannot_be_resolved : given.a_generate_screenplay_command
{
    int _result;

    void Establish() => _settings.Path = Path.Combine(_folder, "does-not-exist");

    async Task Because() => _result = await Execute();

    [Fact] void should_report_that_nothing_was_found() => _result.ShouldEqual(ExitCodes.NotFound);
    [Fact] void should_not_generate() => _generation.DidNotReceive().Generate(Arg.Any<string>(), Arg.Any<ScreenplayGenerationOptions>(), Arg.Any<CancellationToken>());
    [Fact] void should_not_write_anything_to_standard_output() => _standardOutput.ToArray().ShouldBeEmpty();
}
