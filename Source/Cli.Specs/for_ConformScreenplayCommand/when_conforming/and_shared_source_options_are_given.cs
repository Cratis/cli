// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConformScreenplayCommand.when_conforming;

[Collection(CliSpecsCollection.Name)]
public class and_shared_source_options_are_given : given.a_conform_command
{
    void Establish()
    {
        _settings.Project = _project;
        _settings.Domain = "Override";
        _settings.Provider = "arc";
        _settings.Framework = "net10.0";
        _settings.Module = "Registration";
        _settings.FeatureRoot = "Features";
        _settings.SkipSegments = 2;
        _settings.ModulesFromNamespaceRoots = true;
        _settings.AuthoringOnlyConstructs = true;
    }
    async Task Because() => await Execute();
    [Fact] void should_forward_all_shared_options() => _options.ShouldEqual(new ScreenplayGenerationOptions("Override", "Registration", 2, true, "arc") { TargetFramework = "net10.0", FeatureRoot = "Features", AuthoringOnlyConstructs = true });
    [Fact] void should_use_the_explicit_project() => _generation.Received(1).Generate(_project, Arg.Any<ScreenplayGenerationOptions>(), Arg.Any<Action<string>>(), Arg.Any<CancellationToken>());
}
