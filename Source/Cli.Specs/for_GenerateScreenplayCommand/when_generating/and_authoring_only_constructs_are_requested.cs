// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating;

[Collection(CliSpecsCollection.Name)]
public class and_authoring_only_constructs_are_requested : given.a_generate_screenplay_command
{
    void Establish() => _settings.AuthoringOnlyConstructs = true;

    async Task Because() => await Execute();

    [Fact] void should_pass_the_option_to_generation() => _generation.Received(1).Generate(
        Arg.Any<string>(),
        Arg.Is<ScreenplayGenerationOptions>(options => options.AuthoringOnlyConstructs),
        Arg.Any<Action<string>>(),
        Arg.Any<CancellationToken>());
}
