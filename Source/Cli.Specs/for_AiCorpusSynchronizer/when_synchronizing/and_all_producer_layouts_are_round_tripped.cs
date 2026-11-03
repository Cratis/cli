// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

public class and_all_producer_layouts_are_round_tripped : given.a_corpus_for_all_harnesses
{
    readonly List<SyncResult> _results = [];
    AiStatus _status = null!;
    string _commandTarget = null!;
    string _instructionsTarget = null!;
    string _nestedTarget = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "mcp-servers.json"), "{\"schemaVersion\":\"1.0\",\"servers\":[]}");
        foreach (var relative in new[] { "prompts/first", "prompts/second", "harnesses/pi/agents", "harnesses/opencode/agents", "harnesses/copilot/instructions", "hooks" })
        {
            var path = Path.Combine(_corpus, ".cratis", "ai", relative);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, relative.StartsWith("prompts/", StringComparison.Ordinal) ? "example.prompt.md" : "example.md"), "# Example");
        }
        Directory.CreateDirectory(Path.Combine(_project, ".cratis"));
        File.WriteAllText(Path.Combine(_project, ".cratis", "PROJECT.md"), "# Project\n\n## Conventions\n\nLocal guidance\n");
        File.WriteAllText(Path.Combine(_project, "CLAUDE.md"), "user instructions");
    }

    void Because()
    {
        var configuration = AllHarnesses;
        _results.Add(AiCorpusSynchronizer.Synchronize(_project, _corpus, configuration));
        _status = AiCorpusSynchronizer.Status(_project);
        _commandTarget = new FileInfo(Path.Combine(_project, ".pi", "prompts", "example.md")).LinkTarget;
        _instructionsTarget = new FileInfo(Path.Combine(_project, "AGENTS.md")).LinkTarget;
        _nestedTarget = new FileInfo(Path.Combine(_project, ".claude", "CLAUDE.md")).LinkTarget;
        _results.Add(AiCorpusSynchronizer.Synchronize(_project, _corpus, configuration, dryRun: true));
        _results.Add(AiCorpusSynchronizer.Synchronize(_project, _corpus, configuration));
        _results.Add(AiCorpusSynchronizer.Uninstall(_project, dryRun: true));
        _results.Add(AiCorpusSynchronizer.Uninstall(_project));
    }

    [Fact] void should_accept_all_actual_producer_layouts() => _results.TrueForAll(result => result.Conflicts.Count == 0).ShouldBeTrue();
    [Fact] void should_accept_the_recorded_first_entry_target() => _status.ModifiedFiles.ShouldBeEmpty();
    [Fact] void should_keep_a_single_canonical_command_target() => _commandTarget.StartsWith("../../.cratis/ai/prompts/", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_use_the_project_root_instruction_target() => _instructionsTarget.ShouldEqual(".cratis/ai/rules/project.md");
    [Fact] void should_use_the_project_nested_instruction_target() => _nestedTarget.ShouldEqual("../.cratis/ai/rules/project.md");
    [Fact] void should_preserve_unrelated_instructions() => File.ReadAllText(Path.Combine(_project, "CLAUDE.md")).ShouldEqual("user instructions");
    [Fact] void should_preserve_project_owned_concerns() => File.Exists(Path.Combine(_project, ".cratis", "ai", "rules", "project", "conventions.md")).ShouldBeTrue();
}
