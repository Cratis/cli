// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_model_root;

public class with_a_baseline_but_no_identities : for_ScreenplayPlanning.given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(_folder, ".screenplay")).FullName, "release-baseline.json"), "{}");
    async Task Because() => _result = await Plan(_folder);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_the_unavailable_baseline_warning() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-BASELINE-004" && diagnostic.Message.Contains("PATH renders need persisted .screenplay/identities.json", StringComparison.Ordinal));
    [Fact] void should_not_create_identities() => File.Exists(Path.Combine(_folder, ".screenplay", "identities.json")).ShouldBeFalse();
}
