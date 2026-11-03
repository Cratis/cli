// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_configuring_copilot;

public class and_commands_are_not_wanted : Specification
{
    string _tempDir = null!;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    void Because() => AiToolConfigurator.Configure(AiTool.Copilot, _tempDir, new(Force: false, IncludeCommands: false, IncludeContext: true, LlmContextJson: "{}"));

    [Fact] void should_not_create_the_prompts_folder() => Directory.Exists(Path.Combine(_tempDir, ".github", "prompts")).ShouldBeFalse();
    [Fact] void should_not_create_the_legacy_folder() => Directory.Exists(Path.Combine(_tempDir, ".github", "copilot")).ShouldBeFalse();
    [Fact] void should_not_create_the_skills_folder() => Directory.Exists(Path.Combine(_tempDir, ".github", "skills")).ShouldBeFalse();
    [Fact] void should_still_reference_chronicle_from_the_instructions() => File.ReadAllText(Path.Combine(_tempDir, ".github", "copilot-instructions.md")).ShouldContain("@CHRONICLE.md");

    void Destroy()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
