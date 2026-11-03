// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_configuring_copilot;

/// <summary>
/// A prompt a user already keeps in the folder earlier versions wrote to is theirs. Init never moves,
/// deletes or overwrites it - even when forced - and says that it left it alone.
/// </summary>
public class and_a_legacy_prompt_exists : Specification
{
    const string Legacy = "# edited by the user\n";

    string _tempDir = null!;
    string _legacyPath = null!;
    IReadOnlyList<string> _actions = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _legacyPath = Path.Combine(_tempDir, ".github", "copilot", "prompts", "chronicle-diagnose.prompt.md");
        Directory.CreateDirectory(Path.GetDirectoryName(_legacyPath)!);
        File.WriteAllText(_legacyPath, Legacy);
    }

    void Because() => _actions = AiToolConfigurator.Configure(AiTool.Copilot, _tempDir, new(Force: true, IncludeCommands: true, IncludeContext: true, LlmContextJson: "{}"));

    [Fact] void should_leave_the_legacy_prompt_untouched() => File.ReadAllText(_legacyPath).ShouldEqual(Legacy);
    [Fact] void should_write_the_prompt_where_vs_code_looks_for_it() => File.ReadAllText(Path.Combine(_tempDir, ".github", "prompts", "chronicle-diagnose.prompt.md")).ShouldEqual(SlashCommands.ChronicleDiagnose);
    [Fact] void should_say_the_legacy_prompt_was_left_in_place() => _actions.ShouldContain("Left .github/copilot/prompts/chronicle-diagnose.prompt.md unchanged - VS Code looks for prompt files in .github/prompts by default");
    [Fact] void should_not_report_a_move_or_removal() => _actions.Any(action => new[] { "Moved", "Migrated", "Removed", "Deleted" }.Any(verb => action.StartsWith(verb, StringComparison.Ordinal))).ShouldBeFalse();

    void Destroy()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
