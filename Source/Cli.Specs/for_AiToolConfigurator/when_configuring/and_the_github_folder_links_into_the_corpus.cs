// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiToolConfigurator.when_configuring;

public class and_the_github_folder_links_into_the_corpus : Specification
{
    string _project = null!;
    string _corpusCopilot = null!;
    IReadOnlyList<string> _actions = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpusCopilot = Path.Combine(_project, ".cratis", "ai", "harnesses", "copilot");
        Directory.CreateDirectory(_corpusCopilot);
        Directory.CreateSymbolicLink(Path.Combine(_project, ".github"), Path.Combine(".cratis", "ai", "harnesses", "copilot"));
    }

    void Because() => _actions = AiToolConfigurator.Configure(AiTool.Copilot, _project, new(Force: true, IncludeCommands: true, IncludeContext: true, LlmContextJson: "{}"));

    [Fact] void should_not_write_anything_into_the_corpus() => Directory.EnumerateFileSystemEntries(_corpusCopilot).ShouldBeEmpty();
    [Fact] void should_report_the_instructions_as_managed() => IsReportedAsManaged(".github/copilot-instructions.md").ShouldBeTrue();
    [Fact] void should_report_the_prompt_as_managed() => IsReportedAsManaged(".github/prompts/chronicle-diagnose.prompt.md").ShouldBeTrue();
    [Fact] void should_report_the_skill_as_managed() => IsReportedAsManaged(".github/skills/chronicle-cli/SKILL.md").ShouldBeTrue();

    void Destroy() => Directory.Delete(_project, true);

    bool IsReportedAsManaged(string file) => _actions.Any(action => action.Contains(file, StringComparison.Ordinal) && action.Contains("managed corpus", StringComparison.Ordinal));
}
