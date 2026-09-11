// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_pi_cursor_and_opencode_are_selected : Specification
{
    string _project = null!;
    string _corpus = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_corpus, "distribution"));
        foreach (var directory in new[]
        {
            Path.Combine(_corpus, ".cratis", "ai", "rules"),
            Path.Combine(_corpus, ".cratis", "ai", "agents"),
            Path.Combine(_corpus, ".cratis", "ai", "prompts"),
            Path.Combine(_corpus, ".cratis", "ai", "skills", "example"),
            Path.Combine(_corpus, ".cratis", "ai", "harnesses", "pi", "extensions"),
            Path.Combine(_corpus, ".cratis", "ai", "harnesses", "cursor", "rules"),
        })
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"pi\",\"cursor\",\"opencode\"],\"profiles\":[\"cratis/example\"],\"languages\":[\"csharp\"]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/example\",\"availableTargets\":[\"example\"]}],\"engineeringProfiles\":[]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "rules", "general.md"), "# Rule");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "agents", "reviewer.md"), "# Agent");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "prompts", "review.prompt.md"), "---\ndescription: Review\n---\nReview this.");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "skills", "example", "SKILL.md"), "---\nname: example\ndescription: Example\n---\n# Skill");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "harnesses", "pi", "extensions", "index.ts"), "export default () => {};");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "harnesses", "cursor", "rules", "cratis.mdc"), "---\nalwaysApply: true\n---\nUse Cratis.");
    }

    void Because() => AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["pi", "cursor", "opencode"], ["cratis/example"], ["csharp"]));

    [Fact] void should_configure_pi_skills() => new DirectoryInfo(Path.Combine(_project, ".pi", "skills")).LinkTarget.ShouldEqual("../.cratis/ai/skills");
    [Fact] void should_configure_pi_agents() => new DirectoryInfo(Path.Combine(_project, ".pi", "agents")).LinkTarget.ShouldEqual("../.cratis/ai/agents");
    [Fact] void should_configure_pi_extensions() => new DirectoryInfo(Path.Combine(_project, ".pi", "extensions")).LinkTarget.ShouldEqual("../.cratis/ai/harnesses/pi/extensions");
    [Fact] void should_configure_pi_prompts() => new FileInfo(Path.Combine(_project, ".pi", "prompts", "review.md")).LinkTarget.ShouldEqual("../../.cratis/ai/prompts/review.prompt.md");
    [Fact] void should_configure_cursor_rules() => new DirectoryInfo(Path.Combine(_project, ".cursor", "rules")).LinkTarget.ShouldEqual("../.cratis/ai/harnesses/cursor/rules");
    [Fact] void should_configure_cursor_skills() => new DirectoryInfo(Path.Combine(_project, ".cursor", "skills")).LinkTarget.ShouldEqual("../.cratis/ai/skills");
    [Fact] void should_configure_opencode_agents() => new DirectoryInfo(Path.Combine(_project, ".opencode", "agents")).LinkTarget.ShouldEqual("../.cratis/ai/agents");
    [Fact] void should_configure_opencode_commands() => new FileInfo(Path.Combine(_project, ".opencode", "commands", "review.md")).LinkTarget.ShouldEqual("../../.cratis/ai/prompts/review.prompt.md");
    [Fact] void should_share_root_instructions() => new FileInfo(Path.Combine(_project, "AGENTS.md")).LinkTarget.ShouldEqual(".cratis/ai/rules/general.md");
    [Fact] void should_keep_skill_frontmatter_first() => File.ReadAllText(Path.Combine(_project, ".cratis", "ai", "skills", "example", "SKILL.md")).StartsWith("---\n", StringComparison.Ordinal).ShouldBeTrue();

    void Destroy()
    {
        Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
