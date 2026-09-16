// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiToolConfigurator.when_configuring;

public class and_the_paths_are_managed_by_the_corpus : Specification
{
    string _project = null!;
    string _corpusInstructions = null!;
    string _corpusSkills = null!;
    string _instructionsContent = null!;
    IReadOnlyList<string> _claudeActions = null!;
    IReadOnlyList<string> _piActions = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var managedRoot = Path.Combine(_project, ".cratis", "ai");
        _corpusSkills = Path.Combine(managedRoot, "skills");
        Directory.CreateDirectory(_corpusSkills);

        _corpusInstructions = Path.Combine(managedRoot, "rules", "general.md");
        Directory.CreateDirectory(Path.GetDirectoryName(_corpusInstructions)!);
        File.WriteAllText(_corpusInstructions, "# Managed corpus instructions\n");
        _instructionsContent = File.ReadAllText(_corpusInstructions);

        // What 'cratis ai install' creates: instruction files and resource folders as links into the corpus.
        File.CreateSymbolicLink(Path.Combine(_project, "CLAUDE.md"), _corpusInstructions);
        File.CreateSymbolicLink(Path.Combine(_project, "AGENTS.md"), _corpusInstructions);
        Directory.CreateDirectory(Path.Combine(_project, ".pi"));
        Directory.CreateSymbolicLink(Path.Combine(_project, ".pi", "skills"), _corpusSkills);
    }

    void Because()
    {
        var configuration = new AiToolConfiguration(Force: false, IncludeCommands: true, IncludeContext: true, LlmContextJson: "{}");
        _claudeActions = AiToolConfigurator.Configure(AiTool.Claude, _project, configuration);
        _piActions = AiToolConfigurator.Configure(AiTool.Pi, _project, configuration);
    }

    [Fact] void should_not_modify_the_managed_instruction_file() => File.ReadAllText(_corpusInstructions).ShouldEqual(_instructionsContent);
    [Fact] void should_not_write_a_skill_into_the_managed_skills_folder() => Directory.EnumerateFileSystemEntries(_corpusSkills).ShouldBeEmpty();
    [Fact] void should_report_claude_md_as_managed() => _claudeActions.Any(action => action.Contains("CLAUDE.md", StringComparison.Ordinal) && action.Contains("managed corpus", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_agents_md_as_managed() => _piActions.Any(action => action.Contains("AGENTS.md", StringComparison.Ordinal) && action.Contains("managed corpus", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_the_pi_skill_as_managed() => _piActions.Any(action => action.Contains(".pi/skills", StringComparison.Ordinal) && action.Contains("managed corpus", StringComparison.Ordinal)).ShouldBeTrue();

    void Destroy() => Directory.Delete(_project, true);
}
