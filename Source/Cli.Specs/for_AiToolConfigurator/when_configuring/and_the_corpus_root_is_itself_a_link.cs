// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiToolConfigurator.when_configuring;

public class and_the_corpus_root_is_itself_a_link : Specification
{
    string _project = null!;
    string _store = null!;
    IReadOnlyList<string> _actions = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _store = Path.Combine(_project, ".cratis", "store");
        Directory.CreateDirectory(Path.Combine(_store, "prompts"));
        Directory.CreateSymbolicLink(Path.Combine(_project, ".cratis", "ai"), "store");
        Directory.CreateDirectory(Path.Combine(_project, ".github"));

        // The prompts folder links straight at the real location behind the linked corpus root.
        Directory.CreateSymbolicLink(Path.Combine(_project, ".github", "prompts"), Path.Combine("..", ".cratis", "store", "prompts"));
    }

    void Because() => _actions = AiToolConfigurator.Configure(AiTool.Copilot, _project, new(Force: true, IncludeCommands: true, IncludeContext: true, LlmContextJson: "{}"));

    [Fact] void should_not_write_a_prompt_behind_the_linked_corpus_root() => Directory.EnumerateFileSystemEntries(Path.Combine(_store, "prompts")).ShouldBeEmpty();
    [Fact] void should_report_the_prompt_as_managed() => _actions.Any(action => action.Contains(".github/prompts/chronicle-diagnose.prompt.md", StringComparison.Ordinal) && action.Contains("managed corpus", StringComparison.Ordinal)).ShouldBeTrue();

    void Destroy() => Directory.Delete(_project, true);
}
