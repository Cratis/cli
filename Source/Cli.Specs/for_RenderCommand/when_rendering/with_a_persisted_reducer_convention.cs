// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_a_persisted_reducer_convention : given.a_reducer_render_command
{
    string[] _before = [];

    async Task Establish()
    {
        var first = await Capture();
        first.ExitCode.ShouldEqual(ExitCodes.Success);
        _before = Snapshot(_destination);
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_succeed() => _output.ExitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_pass_the_persisted_input_to_stage() => Convention.Sha256.ShouldEqual(ReducerConventionInput.Read("MyApp", File.ReadAllText(_configuration)).Sha256);
    [Fact] void should_keep_self_reducing_rendering() => Encoding.UTF8.GetString(Convention.Bytes.AsSpan()).ShouldContain("\"reducerConvention\":\"self\"");
    [Fact] void should_leave_every_artifact_and_the_manifest_unchanged() => Snapshot(_destination).ShouldEqual(_before);
    [Fact] void should_plan_identical_artifacts() => _plans[1].Artifacts!.Digest.ShouldEqual(_plans[0].Artifacts!.Digest);
}
