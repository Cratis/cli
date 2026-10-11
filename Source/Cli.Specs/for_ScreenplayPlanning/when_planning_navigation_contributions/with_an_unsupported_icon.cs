// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_navigation_contributions;

public class with_an_unsupported_icon : given.a_navigation_contribution
{
    ScreenplayRenderPlan[] _results = null!;

    void Establish() => AddProperties("icon work");
    async Task Because() => _results = [await PlanContribution(), await PlanContribution("Workspaces.Tracking.CreateWorkItem")];

    [Fact] void should_refuse_scoped_and_unscoped_plans() => _results.All(result => !result.Success).ShouldBeTrue();
    [Fact] void should_report_only_the_typed_refusal() => _results.All(result => result.Diagnostics.Count(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error && diagnostic.Code == NavigationContributionAdmission.UnsupportedPropertyCode) == 1).ShouldBeTrue();
    [Fact] void should_name_the_contribution_and_property() => _results.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("Workspaces.Tracking.contribute to Navigation[0]", StringComparison.Ordinal) && diagnostic.Message.Contains("property 'icon'", StringComparison.Ordinal) && diagnostic.Message.Contains("bundled Stage renderer", StringComparison.Ordinal))).ShouldBeTrue();
    [Fact] void should_not_plan_artifacts() => _results.All(result => result.Artifacts is null).ShouldBeTrue();
}
