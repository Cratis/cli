// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_observers_are_required : given.captured_reports
{
    void Establish() => _data = new DiagnoseData("localhost:35000", "store", "tenant-one", true, "19.6.1", null, ["store"], 0, 0, 0, 0, 0, 0, null, DateTimeOffset.UtcNow)
    {
        ObserverRequirements =
        [
            new("active [observer]", "store", "tenant-one", DiagnoseObserverRequirementStatus.Satisfied, "Active", "running"),
            new("absent [observer]", "store", "tenant-one", DiagnoseObserverRequirementStatus.Absent, null, "not registered in store/namespace")
        ]
    };

    void Because() => CaptureReports();

    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    [InlineData(OutputFormats.JsonQuiet)]
    [InlineData(OutputFormats.Plain)]
    [InlineData(OutputFormats.Table)]
    [InlineData(OutputFormats.Auto)]
    [InlineData(OutputFormats.Quiet)]
    [InlineData("watch")]
    void should_show_every_required_observer(string format)
    {
        _outputs[format].ShouldContain("active [observer]");
        _outputs[format].ShouldContain("absent [observer]");
        _outputs[format].ShouldContain("tenant-one");
        _outputs[format].ShouldContain("running");
        _outputs[format].ShouldContain("not registered in store/namespace");
    }

    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    [InlineData(OutputFormats.JsonQuiet)]
    void should_serialize_the_additive_results_and_existing_fields(string format)
    {
        using var json = JsonDocument.Parse(_outputs[format]);
        var requirements = json.RootElement.GetProperty("observerRequirements");
        requirements.GetArrayLength().ShouldEqual(2);
        requirements[0].GetProperty("status").GetString().ShouldEqual("satisfied");
        requirements[1].GetProperty("status").GetString().ShouldEqual("absent");
        requirements[1].TryGetProperty("observedState", out _).ShouldBeFalse();
        json.RootElement.GetProperty("findings").GetArrayLength().ShouldEqual(0);
        json.RootElement.GetProperty("checksCouldNotRun").GetArrayLength().ShouldEqual(0);
        json.RootElement.GetProperty("healthy").GetBoolean().ShouldBeFalse();
        json.RootElement.GetProperty("observerRequirementsSatisfied").GetBoolean().ShouldBeFalse();
    }

    [Fact] void should_use_a_plain_requirement_line() => _outputs[OutputFormats.Plain].ShouldContain("observer_requirement=\"absent [observer]\" event_store=store namespace=tenant-one status=absent state= reason=\"not registered in store/namespace\"");
    [Fact] void should_show_required_observers_in_text() => _outputs[OutputFormats.Table].ShouldContain("Required observers");
    [Fact] void should_color_the_successful_watch_requirement_green() => WatchIcons("Required observer").First().Style.Foreground.ShouldEqual(OutputFormatter.Success);
    [Fact] void should_color_the_absent_watch_requirement_red() => WatchIcons("Required observer").Last().Style.Foreground.ShouldEqual(OutputFormatter.Danger);
}
