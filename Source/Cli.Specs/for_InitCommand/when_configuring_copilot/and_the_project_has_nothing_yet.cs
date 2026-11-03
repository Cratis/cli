// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_configuring_copilot;

/// <summary>
/// VS Code discovers Copilot prompt files in .github/prompts by default. Writing them anywhere else
/// produces files Copilot never offers - which looks like success and delivers nothing.
/// </summary>
public class and_the_project_has_nothing_yet : Specification
{
    string _tempDir = null!;
    IReadOnlyList<string> _actions = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    void Because() => _actions = AiToolConfigurator.Configure(AiTool.Copilot, _tempDir, new(Force: false, IncludeCommands: true, IncludeContext: true, LlmContextJson: "{}"));

    [Fact] void should_write_the_prompt_where_vs_code_looks_for_it() => File.ReadAllText(Path.Combine(_tempDir, ".github", "prompts", "chronicle-diagnose.prompt.md")).ShouldEqual(SlashCommands.ChronicleDiagnose);
    [Fact] void should_not_create_the_legacy_folder() => Directory.Exists(Path.Combine(_tempDir, ".github", "copilot")).ShouldBeFalse();
    [Fact] void should_report_the_new_location() => _actions.ShouldContain("Created .github/prompts/chronicle-diagnose.prompt.md");
    [Fact] void should_not_mention_the_legacy_location() => _actions.Any(action => action.Contains(".github/copilot/prompts", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_still_write_the_skill() => File.Exists(Path.Combine(_tempDir, ".github", "skills", "chronicle-cli", "SKILL.md")).ShouldBeTrue();

    void Destroy()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
