// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_there_are_many_required_observers : given.captured_reports
{
    void Establish() => _data = new DiagnoseData("localhost", "store", "tenant-one", true, "19.6.1", null, [], 0, 0, 0, 0, 0, 0, null, DateTimeOffset.UtcNow)
    {
        ObserverRequirements = [.. Enumerable.Range(1, 5).Select(index => new DiagnoseObserverRequirement($"required-{index}", "store", "tenant-one", DiagnoseObserverRequirementStatus.Satisfied, "Active", "running"))]
    };

    void Because() => CaptureReports();

    [Fact] void should_limit_watch_detail() => WatchIcons("Required observer").Count().ShouldEqual(3);
    [Fact] void should_show_watch_overflow() => _outputs["watch"].ShouldContain("2 more");
    [Fact] void should_keep_every_plain_requirement() => _outputs[OutputFormats.Plain].ShouldContain("required-5");
    [Fact] void should_explain_that_state_is_not_freshness() => _outputs[OutputFormats.Table].ShouldContain("state only; not proof of event freshness or delivery");
}
