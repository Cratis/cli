// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_checking;

[Collection(CliSpecsCollection.Name)]
public class with_a_repository_root : given.a_repository_root_render
{
    void Establish() => _settings.Check = true;

    async Task Because() => _output = await Capture();

    [Fact] void should_report_pending_changes() => _output.ExitCode.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_include_the_repository_workflow_in_the_plan() => _output.Stdout.ShouldContain(Workflow);
    [Fact] void should_not_create_the_destination() => Directory.Exists(_destination).ShouldBeFalse();
}
