// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_an_unknown_target : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await Plan(target: "plugin");

    [Fact] void should_not_be_successful() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_create_an_artifact_plan() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_report_the_unknown_target() => _result.Diagnostics.Single().Code.ShouldEqual("CLI-RENDER-001");
}
