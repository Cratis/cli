// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_model_root;

public class with_a_different_application_name : given.a_persisted_model_root
{
    async Task Because() => _result = await _planning.Plan(new(_root, "OtherApplication", "cratis"), CancellationToken.None);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_name_mismatch() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "CLI-RENDER-018" && diagnostic.Message.Contains("'Model'", StringComparison.Ordinal));
    [Fact] void should_not_plan_artifacts() => _requests.ShouldBeEmpty();
}
