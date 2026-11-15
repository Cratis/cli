// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_configuring_copilot;

public class and_the_prompt_already_exists : Specification
{
    const string Existing = "# my own diagnose\n";

    string _tempDir = null!;
    string _promptPath = null!;
    IReadOnlyList<string> _actions = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _promptPath = Path.Combine(_tempDir, ".github", "prompts", "chronicle-diagnose.prompt.md");
        Directory.CreateDirectory(Path.GetDirectoryName(_promptPath));
        File.WriteAllText(_promptPath, Existing);
    }

    void Because() => _actions = AiToolConfigurator.Configure(AiTool.Copilot, _tempDir, new(Force: false, IncludeCommands: true, IncludeContext: true, LlmContextJson: "{}"));

    [Fact] void should_leave_the_prompt_unchanged() => File.ReadAllText(_promptPath).ShouldEqual(Existing);
    [Fact] void should_report_the_prompt_as_skipped() => _actions.ShouldContain(".github/prompts/chronicle-diagnose.prompt.md already exists (skipped, use --force to overwrite)");

    void Destroy()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
