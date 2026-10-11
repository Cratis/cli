// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_navigation_contributions;

public class without_unsupported_properties : given.a_navigation_contribution
{
    ScreenplayRenderPlan _plan = null!;

    async Task Because() => _plan = await PlanContribution();

    [Fact] void should_render_the_existing_contribution() => _plan.Success.ShouldBeTrue();
    [Fact] void should_have_artifacts() => _plan.Artifacts!.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_not_report_unsupported_properties() => _plan.Diagnostics.Where(diagnostic => diagnostic.Code == NavigationContributionAdmission.UnsupportedPropertyCode).ShouldBeEmpty();
}
