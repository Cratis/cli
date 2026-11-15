// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiToolConfigurator.when_configuring;

public class and_the_copilot_prompts_folder_links_into_the_corpus : Specification
{
    string _project = null!;
    string _corpusPrompts = null!;
    IReadOnlyList<string> _actions = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpusPrompts = Path.Combine(_project, ".cratis", "ai", "prompts");
        Directory.CreateDirectory(_corpusPrompts);
        Directory.CreateDirectory(Path.Combine(_project, ".github"));

        // What 'cratis ai install' creates: a relative link from .github/prompts into the corpus.
        Directory.CreateSymbolicLink(Path.Combine(_project, ".github", "prompts"), Path.Combine("..", ".cratis", "ai", "prompts"));
    }

    void Because() => _actions = AiToolConfigurator.Configure(AiTool.Copilot, _project, new(Force: true, IncludeCommands: true, IncludeContext: true, LlmContextJson: "{}"));

    [Fact] void should_not_write_a_prompt_into_the_corpus() => Directory.EnumerateFileSystemEntries(_corpusPrompts).ShouldBeEmpty();
    [Fact] void should_report_the_prompt_as_managed() => _actions.Any(action => action.Contains(".github/prompts/chronicle-diagnose.prompt.md", StringComparison.Ordinal) && action.Contains("managed corpus", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_report_the_prompt_as_created() => _actions.Any(action => action.StartsWith("Created .github/prompts", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_relocate_the_prompt_to_the_legacy_folder() => Directory.Exists(Path.Combine(_project, ".github", "copilot")).ShouldBeFalse();
    [Fact] void should_keep_the_prompts_folder_a_link() => new DirectoryInfo(Path.Combine(_project, ".github", "prompts")).LinkTarget.ShouldNotBeNull();

    void Destroy() => Directory.Delete(_project, true);
}
