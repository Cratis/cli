// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_a_profile_upgrade_for_another_target : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await _planning.Plan(new(_file, "Projects", "axon") { UpgradeProfile = true }, CancellationToken.None);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_unsupported_upgrade() => _result.Diagnostics.ShouldContain(_ => _.Code == RenderProfileVersion.UpgradeNotSupportedCode);
}
