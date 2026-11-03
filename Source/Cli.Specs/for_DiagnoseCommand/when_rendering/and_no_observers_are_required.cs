// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_no_observers_are_required : given.captured_reports
{
    void Establish() => _data = new DiagnoseData("localhost", "store", "tenant-one", true, "19.6.1", null, [], 0, 0, 0, 0, 0, 0, null, DateTimeOffset.UtcNow);

    void Because() => CaptureReports();

    [Theory]
    [InlineData(OutputFormats.Json)]
    [InlineData(OutputFormats.JsonCompact)]
    [InlineData(OutputFormats.JsonQuiet)]
    void should_preserve_the_existing_json_shape(string format)
    {
        using var json = JsonDocument.Parse(_outputs[format]);
        json.RootElement.TryGetProperty("observerRequirements", out _).ShouldBeFalse();
        json.RootElement.TryGetProperty("observerRequirementsSatisfied", out _).ShouldBeFalse();
    }

    [Fact] void should_not_add_plain_records() => _outputs[OutputFormats.Plain].ShouldNotContain("observer_requirement");
    [Fact] void should_not_add_text_rows() => _outputs[OutputFormats.Table].ShouldNotContain("Required observers");
    [Fact] void should_not_add_watch_rows() => _outputs["watch"].ShouldNotContain("Required observer");
}
