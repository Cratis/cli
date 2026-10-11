// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_navigation_contributions;

public class with_multiple_unsupported_properties : given.a_navigation_contribution
{
    ScreenplayRenderPlan[] _results = null!;

    void Establish() => AddProperties("id work-items", "icon work", "presentation rail", "group work", "destination external \"https://example.com\"");
    async Task Because() => _results = [await PlanContribution(), await PlanContribution("Workspaces.Tracking.CreateWorkItem")];

    [Fact] void should_refuse_scoped_and_unscoped_plans() => _results.All(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_every_unsupported_property() => _results.All(result => result.Diagnostics.Count(diagnostic => diagnostic.Code == NavigationContributionAdmission.UnsupportedPropertyCode && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error) == 5).ShouldBeTrue();
    [Fact] void should_name_every_property() => new[] { "id", "icon", "presentation", "group", "destination" }.All(property => _results.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains($"property '{property}'", StringComparison.Ordinal)))).ShouldBeTrue();
    [Fact] void should_not_plan_artifacts() => _results.All(result => result.Artifacts is null).ShouldBeTrue();
}
