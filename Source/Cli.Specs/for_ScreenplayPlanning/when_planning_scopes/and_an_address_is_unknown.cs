// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes;

public class and_an_address_is_unknown : given.a_scoped_model
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await Plan("Tasks.Backlog.Unknown");

    [Fact] void should_refuse_planning() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_typed_code() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain("CLI-RENDER-011");
    [Fact] void should_list_known_siblings() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "CLI-RENDER-011").Message.ShouldContain("'Intake'");
}
