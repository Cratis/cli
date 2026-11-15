// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_checking_status;

public class and_a_historical_pair_is_forged : given.an_ownership_plan
{
    void Establish() => SetManifest($"[{FileEntry("rules/general.md")}]", $"[{LinkEntry(".pi/agents", "../.cratis/ai/rules")}]");
    void Because() => ExerciseAllCommands();

    [Fact] void should_reject_every_lifecycle_variant() => _outcomes.TrueForAll(outcome => outcome.Error is AiMcpConfigurationInvalid).ShouldBeTrue();
    [Fact] void should_reject_before_destination_access() => _outcomes.TrueForAll(outcome => outcome.Accesses == 0).ShouldBeTrue();
}
