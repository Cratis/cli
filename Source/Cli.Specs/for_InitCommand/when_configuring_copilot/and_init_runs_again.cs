// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_configuring_copilot;

public class and_init_runs_again : Specification
{
    string _tempDir = null!;
    AiToolConfiguration _configuration = null!;
    IReadOnlyList<string> _actions = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _configuration = new(Force: false, IncludeCommands: true, IncludeContext: true, LlmContextJson: "{}");
        AiToolConfigurator.Configure(AiTool.Copilot, _tempDir, _configuration);
    }

    void Because() => _actions = AiToolConfigurator.Configure(AiTool.Copilot, _tempDir, _configuration);

    [Fact] void should_skip_the_existing_prompt() => _actions.ShouldContain(".github/prompts/chronicle-diagnose.prompt.md already exists (skipped, use --force to overwrite)");
    [Fact] void should_keep_the_prompt() => File.ReadAllText(Path.Combine(_tempDir, ".github", "prompts", "chronicle-diagnose.prompt.md")).ShouldEqual(SlashCommands.ChronicleDiagnose);
    [Fact] void should_not_create_the_legacy_folder() => Directory.Exists(Path.Combine(_tempDir, ".github", "copilot")).ShouldBeFalse();

    void Destroy()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
