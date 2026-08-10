// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating;

[Collection(CliSpecsCollection.Name)]
public class and_the_modules_are_asked_of_the_namespace_roots : given.a_generate_screenplay_command
{
    void Establish() => _settings.ModulesFromNamespaceRoots = true;

    async Task Because() => await Execute();

    [Fact] void should_pass_it_to_the_generation() => _generation.Received(1).Generate(
        Arg.Any<string>(),
        Arg.Is<ScreenplayGenerationOptions>(options => options.ModulesFromNamespaceRoots),
        Arg.Any<CancellationToken>());
}
