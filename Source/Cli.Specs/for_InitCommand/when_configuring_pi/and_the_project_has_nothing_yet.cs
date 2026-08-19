// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_configuring_pi;

/// <summary>
/// Pi discovers skills from .pi/skills/&lt;name&gt;/SKILL.md and prompts from .pi/prompts, and reads its
/// context from AGENTS.md. Writing anywhere else produces files Pi never loads - which looks like success
/// and delivers nothing.
/// </summary>
public class and_the_project_has_nothing_yet : Specification
{
    string _tempDir;
    IReadOnlyList<string> _actions;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    void Because() => _actions = AiToolConfigurator.Configure(AiTool.Pi, _tempDir, new(Force: false, IncludeCommands: true, IncludeContext: true, LlmContextJson: "{}"));

    [Fact] void should_write_the_skill_where_pi_looks_for_it() =>
        File.Exists(Path.Combine(_tempDir, ".pi", "skills", "chronicle-cli", "SKILL.md")).ShouldBeTrue();

    [Fact] void should_write_the_diagnose_prompt() =>
        File.Exists(Path.Combine(_tempDir, ".pi", "prompts", "chronicle-diagnose.md")).ShouldBeTrue();

    [Fact] void should_reference_chronicle_from_agents_md() =>
        File.ReadAllText(Path.Combine(_tempDir, "AGENTS.md")).ShouldContain("@CHRONICLE.md");

    [Fact] void should_give_the_skill_the_frontmatter_pi_requires() =>
        File.ReadAllText(Path.Combine(_tempDir, ".pi", "skills", "chronicle-cli", "SKILL.md"))
            .ShouldContain("name: chronicle-cli");

    [Fact] void should_report_what_it_did() => _actions.ShouldNotBeEmpty();

    void Destroy()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
