// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_the_corpus_ships_opencode_agent_adapters : Specification
{
    string _project = null!;
    string _corpus = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        foreach (var directory in new[]
        {
            Path.Combine(_corpus, ".cratis", "ai", "rules"),
            Path.Combine(_corpus, ".cratis", "ai", "agents"),
            Path.Combine(_corpus, ".cratis", "ai", "prompts"),
            Path.Combine(_corpus, ".cratis", "ai", "skills", "example"),
            Path.Combine(_corpus, ".cratis", "ai", "harnesses", "opencode", "agents"),
        })
        {
            Directory.CreateDirectory(directory);
        }
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"opencode\"],\"profiles\":[\"cratis/example\"],\"languages\":[\"csharp\"]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/example\",\"availableTargets\":[\"example\"]}],\"engineeringProfiles\":[]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "rules", "general.md"), "# Rule");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "agents", "reviewer.md"), "---\nname: Reviewer\ndescription: Reviews\ntools:\n  - Read\nreadonly: true\n---\n# Agent");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "harnesses", "opencode", "agents", "reviewer.md"), "---\ndescription: Reviews\nmode: subagent\npermission:\n  edit: deny\n  bash: deny\n---\n# Agent");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "prompts", "review.prompt.md"), "---\ndescription: Review\n---\nReview this.");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "skills", "example", "SKILL.md"), "---\nname: example\ndescription: Example\n---\n# Skill");
    }

    void Because() => AiCorpusSynchronizer.Synchronize(_project, _corpus, new(["opencode"], ["cratis/example"], ["csharp"]));

    [Fact] void should_link_opencode_agents_at_the_generated_adapters() => new DirectoryInfo(Path.Combine(_project, ".opencode", "agents")).LinkTarget.ShouldEqual("../.cratis/ai/harnesses/opencode/agents");
    [Fact] void should_install_the_adapter() => File.Exists(Path.Combine(_project, ".cratis", "ai", "harnesses", "opencode", "agents", "reviewer.md")).ShouldBeTrue();
    [Fact] void should_keep_the_adapter_frontmatter_first() => File.ReadAllText(Path.Combine(_project, ".cratis", "ai", "harnesses", "opencode", "agents", "reviewer.md")).StartsWith("---\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_still_install_the_canonical_agent() => File.Exists(Path.Combine(_project, ".cratis", "ai", "agents", "reviewer.md")).ShouldBeTrue();

    void Destroy()
    {
        if (Directory.Exists(_project)) Directory.Delete(_project, true);
        if (Directory.Exists(_corpus)) Directory.Delete(_corpus, true);
    }
}
