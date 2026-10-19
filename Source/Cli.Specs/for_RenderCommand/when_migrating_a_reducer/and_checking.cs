// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_migrating_a_reducer;

[Collection(CliSpecsCollection.Name)]
public class and_checking : given.a_classic_reducer_destination
{
    string[] _before = [];

    void Establish()
    {
        _before = Snapshot(_destination);
        _settings.MigrateReducer = [ReadModel];
        _settings.Check = true;
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_report_changes_pending() => _output.ExitCode.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_report_the_deletion() => _output.Stdout.ShouldContain(_reducerPath);
    [Fact] void should_not_change_the_destination() => Snapshot(_destination).ShouldEqual(_before);
}
