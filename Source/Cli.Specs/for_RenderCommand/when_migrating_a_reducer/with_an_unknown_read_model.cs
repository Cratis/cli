// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_migrating_a_reducer;

[Collection(CliSpecsCollection.Name)]
public class with_an_unknown_read_model : given.a_classic_reducer_destination
{
    string[] _before = [];

    void Establish()
    {
        _before = Snapshot(_destination);
        _settings.MigrateReducer = ["Orders.Ordering.Totals.Missing"];
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_fail_validation() => _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_migration_refusal() => (_output.Stdout + _output.Stderr).ShouldContain("CLI-RENDER-026");
    [Fact] void should_not_change_the_destination() => Snapshot(_destination).ShouldEqual(_before);
}
