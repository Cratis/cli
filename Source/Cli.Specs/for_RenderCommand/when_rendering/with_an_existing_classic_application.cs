// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_an_existing_classic_application : given.a_reducer_render_command
{
    string[] _before = [];

    async Task Establish()
    {
        await PublishClassic();
        _before = Snapshot(_destination);
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_succeed() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_add_a_convention_input() => _requests.Single().Profile.Inputs.Any(input => input.Name == ReducerConventionInput.Name).ShouldBeFalse();
    [Fact] void should_preserve_all_existing_bytes() => Snapshot(_destination).ShouldEqual(_before);
    [Fact] void should_keep_the_separate_reducer() => _plans.Single().Artifacts!.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/Fold.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_publish_configuration() => File.Exists(_configuration).ShouldBeFalse();
}
