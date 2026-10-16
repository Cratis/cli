// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_a_fresh_reducer_destination : given.a_reducer_render_command
{
    void Establish() => _settings.Check = true;

    async Task Because() => _output = await Capture();

    [Fact] void should_report_pending_changes() => _output.ExitCode.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_report_configuration_as_a_pending_write() => _output.Stdout.ShouldContain($"\"path\":\"{ReducerConventionInput.RelativePath}\",\"kind\":\"write\"");
    [Fact] void should_plan_the_configuration() => _plans.Single().Artifacts!.Artifacts.Any(artifact => artifact.RelativePath == ReducerConventionInput.RelativePath).ShouldBeTrue();
    [Fact] void should_not_create_the_destination() => Directory.Exists(_destination).ShouldBeFalse();
}
