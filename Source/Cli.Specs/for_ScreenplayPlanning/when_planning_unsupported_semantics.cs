// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_unsupported_semantics : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => File.WriteAllText(
        _file,
        string.Join(
            '\n',
            [
                "concept ProjectId : Uuid",
                "module Projects",
                "  feature Registration",
                "    slice StateChange RegisterProject",
                "      command RegisterProject",
                "        projectId ProjectId identifier",
                "        produces ProjectRegistered",
                "          projectId = projectId",
                "      event ProjectRegistered",
                "        projectId ProjectId"
            ]));

    async Task Because() => _result = await Plan();

    [Fact] void should_not_be_successful() => _result.Success.ShouldBeFalse();
    [Fact] void should_keep_the_non_publishable_stage_plan() => _result.Artifacts.ShouldNotBeNull();
    [Fact] void should_report_the_blocking_stage_diagnostic() => _result.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-006");
    [Fact] void should_not_plan_semantic_source_artifacts() => _result.Artifacts!.Artifacts.All(_ => !_.RelativePath.Contains("Registration", StringComparison.Ordinal)).ShouldBeTrue();
}
