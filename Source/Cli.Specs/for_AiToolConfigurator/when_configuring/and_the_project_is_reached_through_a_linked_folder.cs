// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiToolConfigurator.when_configuring;

public class and_the_project_is_reached_through_a_linked_folder : Specification
{
    string _root = null!;
    string _project = null!;
    string _corpusInstructions = null!;
    string _corpusPrompts = null!;
    IReadOnlyList<string> _actions = null!;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var real = Path.Combine(_root, "real");
        _corpusInstructions = Path.Combine(real, ".cratis", "ai", "rules", "project.md");
        _corpusPrompts = Path.Combine(real, ".cratis", "ai", "prompts");
        Directory.CreateDirectory(Path.GetDirectoryName(_corpusInstructions)!);
        Directory.CreateDirectory(_corpusPrompts);
        File.WriteAllText(_corpusInstructions, "# corpus\n");

        // The project is only reachable through a link, so its own path contains a link.
        var project = Path.Combine(_root, "linked");
        Directory.CreateSymbolicLink(project, real);
        Directory.CreateDirectory(Path.Combine(project, ".github"));
        File.CreateSymbolicLink(Path.Combine(project, ".github", "copilot-instructions.md"), Path.Combine("..", ".cratis", "ai", "rules", "project.md"));
        Directory.CreateSymbolicLink(Path.Combine(project, ".github", "prompts"), Path.Combine("..", ".cratis", "ai", "prompts"));
        _project = project;
    }

    void Because() => _actions = AiToolConfigurator.Configure(AiTool.Copilot, _project, new(Force: true, IncludeCommands: true, IncludeContext: true, LlmContextJson: "{}"));

    [Fact] void should_not_modify_the_managed_instruction_file() => File.ReadAllText(_corpusInstructions).ShouldEqual("# corpus\n");
    [Fact] void should_not_write_a_prompt_into_the_corpus() => Directory.EnumerateFileSystemEntries(_corpusPrompts).ShouldBeEmpty();
    [Fact] void should_report_the_instructions_as_managed() => IsReportedAsManaged(".github/copilot-instructions.md").ShouldBeTrue();
    [Fact] void should_report_the_prompt_as_managed() => IsReportedAsManaged(".github/prompts/chronicle-diagnose.prompt.md").ShouldBeTrue();

    void Destroy() => Directory.Delete(_root, true);

    bool IsReportedAsManaged(string file) => _actions.Any(action => action.Contains(file, StringComparison.Ordinal) && action.Contains("managed corpus", StringComparison.Ordinal));
}
