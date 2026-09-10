// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_the_context_file_is_generated;

/// <summary>
/// Some repositories generate their instruction files from a shared corpus and propagate them, so appending
/// to one is undone by the next sync - the reference disappears at an unpredictable later moment, which is
/// worse than never adding it. The skill and prompt are unaffected and still written.
/// <para>
/// The skip is reported rather than silent, because a project that looks configured and loads nothing is
/// the harder failure to notice.
/// </para>
/// </summary>
public class and_configuring_pi : Specification
{
    string _tempDir;
    string _agentsMd;
    IReadOnlyList<string> _actions;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _agentsMd = Path.Combine(_tempDir, "AGENTS.md");
        File.WriteAllText(_agentsMd, "# Generated - do not edit\n");
    }

    void Because() => _actions = AiToolConfigurator.Configure(
        AiTool.Pi,
        _tempDir,
        new(Force: false, IncludeCommands: true, IncludeContext: false, LlmContextJson: "{}"));

    [Fact] void should_leave_the_generated_file_untouched() =>
        File.ReadAllText(_agentsMd).ShouldEqual("# Generated - do not edit\n");

    [Fact] void should_still_write_the_skill() =>
        File.Exists(Path.Combine(_tempDir, ".pi", "skills", "chronicle-cli", "SKILL.md")).ShouldBeTrue();

    [Fact] void should_still_write_the_prompt() =>
        File.Exists(Path.Combine(_tempDir, ".pi", "prompts", "chronicle-diagnose.md")).ShouldBeTrue();

    [Fact] void should_say_it_skipped_the_reference() =>
        _actions.ShouldContain(_ => _.Contains("Skipped the @CHRONICLE.md reference in AGENTS.md", StringComparison.Ordinal));

    void Destroy()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
