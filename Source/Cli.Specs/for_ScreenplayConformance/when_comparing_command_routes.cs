// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;

namespace Cratis.Cli.for_ScreenplayConformance;

public class when_comparing_command_routes : Specification
{
    ModelDifference _difference = null!;
    IReadOnlyList<ConformanceFinding> _findings = [];

    void Because()
    {
        const string source = for_ScreenplayPlanning.when_planning_an_esm_v8_model.RoutedSource;
        var model = new AuthoredScreenplay("Banking", new Dictionary<string, string> { ["Routes.play"] = source }, []);
        _difference = new ScreenplayConformance().Compare(model, source.Replace("streamId = month", "streamId = 1", StringComparison.Ordinal));
        _findings = ScreenplayConformance.Classify(_difference);
    }

    [Fact] void should_report_the_changed_command_route() => _findings.Any(finding => finding.Kind == "Command" && finding.Category == "ShapeMismatch").ShouldBeTrue();
    [Fact] void should_not_silently_pass_a_different_route() => ScreenplayConformance.ExitCode(_difference, _findings).ShouldEqual(1);
}
