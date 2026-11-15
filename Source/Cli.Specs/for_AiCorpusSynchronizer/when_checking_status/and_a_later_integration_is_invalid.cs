// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_checking_status;

public class and_a_later_integration_is_invalid : given.an_ownership_plan
{
    void Because()
    {
        var invalid = new[]
        {
            LinkEntry(".pi/skills", null), LinkEntry(null, "../.cratis/ai/skills"),
            LinkEntry(".pi/skills", ""), LinkEntry(".pi/skills", "../.cratis/instructions"),
            LinkEntry(".agents/agents", "../.cratis/ai/agents"), LinkEntry(".pi/prompts", "../.cratis/ai/prompts"),
            LinkEntry(".pi/skills", "../.cratis/ai/skills", false), LinkEntry("../outside", "../.cratis/ai/skills"),
            LinkEntry(".github/instructions", "../.cratis/ai/agents"), "null", "{}",
            """{"Target":"../.cratis/ai/skills","IsDirectory":true}""",
            """{"Path":".pi/skills","IsDirectory":true}""",
            LinkEntry(".pi/skills", Path.Combine(_corpus, "sentinel.txt"))
        };
        foreach (var entry in invalid)
        {
            SetManifest($"[{FileEntry("rules/general.md")}]", $"[{LinkEntry(".pi/agents", "../.cratis/ai/agents")},{entry}]");
            ExerciseAllCommands();
        }
    }

    [Fact] void should_exercise_every_lifecycle_variant() => _outcomes.Count.ShouldEqual(126);
    [Fact] void should_reject_every_plan() => _outcomes.TrueForAll(outcome => outcome.Error is AiMcpConfigurationInvalid).ShouldBeTrue();
    [Fact] void should_not_access_the_valid_earlier_entries() => _outcomes.TrueForAll(outcome => outcome.Accesses == 0).ShouldBeTrue();
    [Fact] void should_preserve_the_earlier_link() => new DirectoryInfo(Path.Combine(_project, ".pi", "agents")).LinkTarget.ShouldEqual("../.cratis/ai/agents");
}
