// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_configuring_copilot;

public class and_forcing_over_an_existing_prompt : Specification
{
    string _tempDir = null!;
    string _promptPath = null!;
    IReadOnlyList<string> _actions = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _promptPath = Path.Combine(_tempDir, ".github", "prompts", "chronicle-diagnose.prompt.md");
        Directory.CreateDirectory(Path.GetDirectoryName(_promptPath));
        File.WriteAllText(_promptPath, "# my own diagnose\n");
    }

    void Because() => _actions = AiToolConfigurator.Configure(AiTool.Copilot, _tempDir, new(Force: true, IncludeCommands: true, IncludeContext: true, LlmContextJson: "{}"));

    [Fact] void should_overwrite_the_prompt() => File.ReadAllText(_promptPath).ShouldEqual(SlashCommands.ChronicleDiagnose);
    [Fact] void should_report_the_prompt_as_created() => _actions.ShouldContain("Created .github/prompts/chronicle-diagnose.prompt.md");

    void Destroy()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
