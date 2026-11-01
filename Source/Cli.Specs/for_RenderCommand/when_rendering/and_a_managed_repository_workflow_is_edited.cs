// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_a_managed_repository_workflow_is_edited : given.a_repository_root_render
{
    string[] _before = [];

    async Task Establish()
    {
        (await Capture()).ExitCode.ShouldEqual(ExitCodes.Success);
        await File.AppendAllTextAsync(_workflow, "# local edit\n", TestContext.Current.CancellationToken);
        _before = Snapshot(_destination);
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_refuse_the_modified_managed_file() => _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_leave_every_file_untouched() => Snapshot(_destination).ShouldEqual(_before);
}
