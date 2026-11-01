// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes;

public class and_an_address_is_ambiguous : given.a_scoped_model
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => File.WriteAllText(_taskFile, TaskSource + "\n    slice StateChange Intake\n      command StartTask\n        taskId Uuid identifier\n        produces TaskStarted\n          for taskId\n      event TaskStarted\n");
    async Task Because() => _result = await Plan("Tasks.Backlog.Intake");

    [Fact] void should_refuse_planning() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_ambiguity() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("CLI-RENDER-012");
}
