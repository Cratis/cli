// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_copilot_ships_rule_copies : given.a_copilot_corpus
{
    void Establish()
    {
        AddAdapter();
        var root = Path.Combine(_corpus, ".cratis", "ai");
        File.WriteAllText(Path.Combine(root, "manifest.json"), """{"harnesses":["copilot"],"profiles":["cratis/application/csharp","cratis/engineering/csharp"],"languages":["csharp"]}""");
        File.WriteAllText(Path.Combine(root, "profile-catalog.json"), """{"publicProfiles":[{"id":"cratis/application/csharp"}],"engineeringProfiles":[{"id":"cratis/engineering/csharp"}]}""");
        File.WriteAllText(Path.Combine(root, "rules", "framework.md"), "---\napplyTo: \"**/*.cs\"\nprofile: framework\n---\n# Framework rule");

        var instructions = Path.Combine(root, "harnesses", "copilot", "instructions");
        File.WriteAllText(Path.Combine(instructions, "framework.instructions.md"), "---\napplyTo: \"**/*.cs\"\n---\n# Framework rule");
        File.WriteAllText(Path.Combine(instructions, "orphan.instructions.md"), "---\napplyTo: \"**/*\"\n---\n# Orphan rule");
        File.WriteAllText(Path.Combine(instructions, "notes.md"), "# Notes");
    }

    [Theory]
    [InlineData("cratis/application/csharp", "framework.instructions.md", false)]
    [InlineData("cratis/engineering/csharp", "framework.instructions.md", true)]
    [InlineData("cratis/engineering/csharp", "orphan.instructions.md", false)]
    [InlineData("cratis/application/csharp", "notes.md", true)]
    void should_filter_only_instruction_copies_without_a_selected_rule(string profile, string instruction, bool expected)
    {
        AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["copilot"], [profile], ["csharp"]));
        File.Exists(Path.Combine(_project, ".github", "instructions", instruction)).ShouldEqual(expected);
    }
}
