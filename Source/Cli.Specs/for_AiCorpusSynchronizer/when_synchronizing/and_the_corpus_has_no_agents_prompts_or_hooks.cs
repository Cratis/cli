// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_the_corpus_has_no_agents_prompts_or_hooks : given.a_corpus_with_profile_scoped_rules
{
    static readonly string[] _harnesses = ["claude", "codex", "copilot", "pi", "cursor", "opencode"];
    SyncResult _result = null!;
    IReadOnlyList<AiManagedIntegration> _links = null!;

    void Establish()
    {
        var root = Path.Combine(_corpus, ".cratis", "ai");
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "manifest.json")));
        manifest["harnesses"] = JsonSerializer.SerializeToNode(_harnesses);
        File.WriteAllText(Path.Combine(root, "manifest.json"), manifest.ToJsonString());
        File.WriteAllText(Path.Combine(root, "profile-catalog.json"), """{"publicProfiles":[{"id":"cratis/example","availableTargets":["example"]}],"engineeringProfiles":[]}""");
        Directory.CreateDirectory(Path.Combine(root, "skills", "example"));
        File.WriteAllText(Path.Combine(root, "skills", "example", "SKILL.md"), "---\nname: example\ndescription: Example\n---\n# Skill");
    }

    void Because()
    {
        _result = AiCorpusSynchronizer.Synchronize(_project, _corpus, new(_harnesses, ["cratis/example"], []));
        _links = JsonSerializer.Deserialize<AiInstallationManifest>(File.ReadAllText(Path.Combine(_project, ".cratis", "ai.manifest.json"))).Integrations;
    }

    [Fact] void should_not_report_a_conflict() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_create_no_link_without_a_target() => _links.Where(link => !Path.Exists(Path.Combine(_project, link.Path))).ShouldBeEmpty();
    [Fact] void should_link_the_skills_for_every_harness() => new[] { ".claude", ".agents", ".github", ".pi", ".cursor", ".opencode" }.Select(harness => new DirectoryInfo(Path.Combine(_project, harness, "skills")).LinkTarget).Distinct().ShouldEqual(["../.cratis/ai/skills"]);
    [Fact] void should_leave_no_agents_link() => AiCorpusSynchronizer.PathExists(Path.Combine(_project, ".claude", "agents")).ShouldBeFalse();
    [Fact] void should_report_no_drift_afterwards() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
}
