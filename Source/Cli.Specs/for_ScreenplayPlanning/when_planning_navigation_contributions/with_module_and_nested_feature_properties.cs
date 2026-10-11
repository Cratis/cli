// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_navigation_contributions;

#pragma warning disable MA0136 // Fixture line endings do not affect normalized rendered artifacts.

public class with_module_and_nested_feature_properties : given.a_navigation_contribution
{
    ScreenplayRenderPlan _plan = null!;

    void Establish()
    {
        var path = Path.Combine(_folder, "Workspaces", "Tracking", "Tracking.play");
        var source = File.ReadAllText(path).Replace("module Workspaces", "module Workspaces\n  contribute to Navigation\n    label \"Workspaces\"\n    icon work", StringComparison.Ordinal);
        File.WriteAllText(path, $"{source.TrimEnd()}\n" + """
                feature Nested
                  contribute to Navigation
                    label "Outside"
                    destination external "https://example.com"
            """);
    }

    async Task Because() => _plan = await PlanContribution("Workspaces.Tracking.CreateWorkItem");

    [Fact] void should_refuse_properties_even_outside_the_selected_slice() => _plan.Success.ShouldBeFalse();
    [Fact] void should_check_the_module_contribution() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == NavigationContributionAdmission.UnsupportedPropertyCode && diagnostic.Message.Contains("Workspaces.contribute to Navigation[0]", StringComparison.Ordinal) && diagnostic.Message.Contains("property 'icon'", StringComparison.Ordinal));
    [Fact] void should_check_the_nested_feature_contribution() => _plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == NavigationContributionAdmission.UnsupportedPropertyCode && diagnostic.Message.Contains("Workspaces.Tracking.Nested.contribute to Navigation[0]", StringComparison.Ordinal) && diagnostic.Message.Contains("property 'destination'", StringComparison.Ordinal));
    [Fact] void should_not_plan_artifacts() => _plan.Artifacts.ShouldBeNull();
}
