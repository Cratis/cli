// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_migrating_a_reducer;

[Collection(CliSpecsCollection.Name)]
public class and_the_reducer_changes_after_planning : given.a_classic_reducer_destination
{
    string[] _edited = [];

    void Establish()
    {
        var planning = new ScreenplayPlanning();
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            var plan = await planning.Plan(call.Arg<ScreenplayRenderRequest>(), call.Arg<CancellationToken>());
            await File.AppendAllTextAsync(ReducerFile, "// edited after planning\n");
            _edited = Snapshot(_destination);
            return plan;
        });
        _settings.MigrateReducer = [ReadModel];
    }

    async Task Because() => _output = await Capture();

    [Fact] void should_fail_validation() => _output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_unsafe_removal() => (_output.Stdout + _output.Stderr).ShouldContain("CLI-RENDER-025");
    [Fact] void should_not_write_or_delete_anything() => Snapshot(_destination).ShouldEqual(_edited);
}
