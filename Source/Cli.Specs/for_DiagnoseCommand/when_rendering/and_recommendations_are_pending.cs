// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_recommendations_are_pending : given.captured_reports
{
    void Establish() => _data = _data with { PendingRecommendations = 1 };

    void Because() => CaptureReports();

    [Fact] void should_keep_the_warning_icon_for_completed_recommendations_in_watch() => _outputs["watch"].Split('\n').Single(x => x.Contains("Recommendations", StringComparison.Ordinal)).ShouldContain("▲");
}
