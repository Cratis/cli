// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_navigation_contributions;

public class with_authored_navigation_metadata : given.a_navigation_contribution
{
    ScreenplayRenderPlan[] _results = null!;

    void Establish() => AddProperties("id work-items", "icon work", "presentation rail", "group work", "destination external \"https://example.com\"");
    async Task Because() => _results = [await PlanContribution(), await PlanContribution("Workspaces.Tracking.CreateWorkItem")];

    [Fact] void should_render_scoped_and_unscoped_plans() => _results.All(result => result.Success).ShouldBeTrue();
    [Fact] void should_report_no_error() => _results.SelectMany(result => result.Diagnostics).Any(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeFalse();
    [Fact]
    void should_carry_the_metadata_in_the_planned_scene() => new[] { "\"id\":\"work-items\"", "\"icon\":\"work\"", "\"presentation\":\"rail\"", "\"group\":\"work\"", "https://example.com" }
        .All(text => Scene(_results[0]).Contains(text, StringComparison.Ordinal)).ShouldBeTrue();

    static string Scene(ScreenplayRenderPlan plan) => Encoding.UTF8.GetString(plan.Artifacts!.Artifacts.Single(artifact => string.Equals(artifact.RelativePath, "scene.json", StringComparison.Ordinal)).Bytes.AsSpan());
}
