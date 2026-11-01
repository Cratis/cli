// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_a_fresh_reducer_destination : given.a_reducer_render_command
{
    void Establish()
    {
        Directory.CreateDirectory(_destination);
        File.WriteAllText(Path.Combine(_destination, "repository-notes.txt"), "Existing repository, never rendered.");
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_succeed() => Assert.True(_output.ExitCode == ExitCodes.Success, _output.Stderr);
    [Fact] void should_plan_the_self_convention() => Encoding.UTF8.GetString(Convention.Bytes.AsSpan()).ShouldContain("\"reducerConvention\":\"self\"");
    [Fact] void should_publish_the_self_convention() => File.ReadAllText(_configuration).ShouldContain("\"reducerConvention\":\"self\"");
    [Fact] void should_own_the_configuration() => File.ReadAllText(Path.Combine(_destination, ".cratis-render.json")).ShouldContain(ReducerConventionInput.RelativePath);
    [Fact] void should_render_static_reduce_on_the_read_model() => Text(_plans.Single().Artifacts!.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("/Totals.cs", StringComparison.Ordinal))).ShouldContain(" Reduce(");
    [Fact] void should_not_emit_a_separate_reducer() => _plans.Single().Artifacts!.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/Fold.cs", StringComparison.Ordinal)).ShouldBeFalse();
}
