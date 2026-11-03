// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_checking_status;

public class and_destinations_collide : given.an_ownership_plan
{
    void Because()
    {
        SetManifest($"[{FileEntry("rules/general.md")},{FileEntry("rules/general.md")}]", "[]");
        ExerciseAllCommands();
        SetManifest($"[{FileEntry("rules")},{FileEntry("rules/general.md")}]", "[]");
        ExerciseAllCommands();
        SetManifest("[]", $"[{LinkEntry(".pi/skills", "../.cratis/ai/skills")},{LinkEntry(".pi/skills", "../.cratis/ai/skills")}]");
        ExerciseAllCommands();
    }

    [Fact] void should_reject_all_duplicate_and_prefix_collisions() => _outcomes.TrueForAll(outcome => outcome.Error is AiMcpConfigurationInvalid).ShouldBeTrue();
    [Fact] void should_cover_all_commands() => _outcomes.Count.ShouldEqual(27);
    [Fact] void should_not_access_destinations() => _outcomes.TrueForAll(outcome => outcome.Accesses == 0).ShouldBeTrue();
}
