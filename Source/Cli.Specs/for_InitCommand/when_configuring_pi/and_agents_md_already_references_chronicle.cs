// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InitCommand.when_configuring_pi;

/// <summary>
/// AGENTS.md is shared with other tools and is frequently hand-maintained, so configuring Pi has to be
/// idempotent against it - running init twice must not stack duplicate references into somebody's file.
/// </summary>
public class and_agents_md_already_references_chronicle : Specification
{
    string _tempDir;
    string _agentsMd;

    void Establish()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _agentsMd = Path.Combine(_tempDir, "AGENTS.md");
        File.WriteAllText(_agentsMd, "# House rules\n\n@CHRONICLE.md\n");
    }

    void Because() => AiToolConfigurator.Configure(AiTool.Pi, _tempDir, new(Force: false, IncludeCommands: false, IncludeContext: true, LlmContextJson: "{}"));

    [Fact] void should_not_add_a_second_reference() =>
        File.ReadAllText(_agentsMd).Split("@CHRONICLE.md").Length.ShouldEqual(2);

    [Fact] void should_leave_the_existing_content_alone() =>
        File.ReadAllText(_agentsMd).ShouldContain("# House rules");

    void Destroy()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}
