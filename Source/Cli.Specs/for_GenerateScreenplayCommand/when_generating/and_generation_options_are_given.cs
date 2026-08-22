// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_GenerateScreenplayCommand.when_generating;

[Collection(CliSpecsCollection.Name)]
public class and_generation_options_are_given : given.a_generate_screenplay_command
{
    void Establish()
    {
        _settings.Domain = "Library";
        _settings.Module = "Lending";
        _settings.SkipSegments = 2;
        _settings.Provider = ScreenplayProviders.CritterStack;
        _settings.Framework = "net9.0";
    }

    async Task Because() => await Execute();

    [Fact] void should_pass_them_to_the_generation() => _generation.Received(1).Generate(
        Arg.Any<string>(),
        Arg.Is<ScreenplayGenerationOptions>(options => options.Domain == "Library" && options.Module == "Lending" && options.SegmentsToSkip == 2),
        Arg.Any<CancellationToken>());

    [Fact] void should_pass_the_provider_to_the_generation() => _generation.Received(1).Generate(
        Arg.Any<string>(),
        Arg.Is<ScreenplayGenerationOptions>(options => options.Provider == ScreenplayProviders.CritterStack),
        Arg.Any<CancellationToken>());

    [Fact] void should_pass_the_target_framework_to_the_generation() => _generation.Received(1).Generate(
        Arg.Any<string>(),
        Arg.Is<ScreenplayGenerationOptions>(options => options.TargetFramework == "net9.0"),
        Arg.Any<CancellationToken>());

    [Fact] void should_leave_the_modules_named_by_one_name() => _generation.Received(1).Generate(
        Arg.Any<string>(),
        Arg.Is<ScreenplayGenerationOptions>(options => !options.ModulesFromNamespaceRoots),
        Arg.Any<CancellationToken>());
}
