// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_recommendations_could_not_run : given.captured_reports
{
    void Establish() => _data = _data with
    {
        ChecksCouldNotRun = [new DiagnoseCheckFailure("Recommendations", "store", "tenant-one", "Recommendation query failed")]
    };

    void Because() => CaptureReports();

    [Fact] void should_show_a_failure_icon_for_recommendations_in_watch() => _outputs["watch"].Split('\n').Single(x => x.Contains("Recommendations", StringComparison.Ordinal) && !x.Contains("Could not check", StringComparison.Ordinal)).ShouldContain("✗");
    [Fact] void should_not_show_a_warning_icon_for_recommendations_in_watch() => _outputs["watch"].ShouldNotContain("▲");
}
