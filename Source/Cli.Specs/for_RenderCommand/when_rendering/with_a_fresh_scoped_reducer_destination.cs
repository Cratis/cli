// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_a_fresh_scoped_reducer_destination : given.a_reducer_render_command
{
    void Establish() => _settings.Scope = ["Orders"];

    async Task Because() => _output = await Capture();

    [Fact] void should_succeed() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_use_the_same_input_in_the_scaffold() => _plans.Single().ScaffoldOptions!.Profile!.Inputs.Single(input => input.Name == ReducerConventionInput.Name).ShouldBeSame(Convention);
    [Fact] void should_publish_the_convention_with_the_scaffold() => File.ReadAllText(_configuration).ShouldContain("\"reducerConvention\":\"self\"");
    [Fact] void should_render_the_selected_read_model_as_self_reducing() => Text(_plans.Single().Artifacts!.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Totals.cs", StringComparison.Ordinal))).ShouldContain(" Reduce(");
}
