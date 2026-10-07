// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CompletionsCommand;

public class when_getting_command_tree : Specification
{
    IReadOnlyList<CommandNode> _result;

    List<CommandNode> _walked = [];
    List<CommandNode> _with_duplicate_children = [];

    void Because()
    {
        _result = CliCommandTree.Commands;
        Walk(_result);
    }

    void Walk(IReadOnlyList<CommandNode> nodes)
    {
        foreach (var node in nodes)
        {
            _walked.Add(node);
            if (node.Children.Select(child => child.Name).Distinct().Count() != node.Children.Count)
            {
                _with_duplicate_children.Add(node);
            }
            Walk(node.Children);
        }
    }

    [Fact] void should_exclude_global_options_for_screenplay_mcp() => _result.Single(n => n.Name == "screenplay").Children.Single(n => n.Name == "mcp").IncludesGlobalOptions.ShouldBeFalse();
    [Fact] void should_include_global_options_for_screenplay_generate() => _result.Single(n => n.Name == "screenplay").Children.Single(n => n.Name == "generate").IncludesGlobalOptions.ShouldBeTrue();
    [Fact] void should_walk_a_nonempty_tree() => _walked.Count.ShouldBeGreaterThan(0);
    [Fact] void should_have_distinct_child_names_at_every_node() => _with_duplicate_children.ShouldBeEmpty();

    [Fact] void should_include_chronicle() => _result.ShouldContain(n => n.Name == "chronicle");
    [Fact] void should_include_context() => _result.ShouldContain(n => n.Name == "context");
    [Fact] void should_include_completions() => _result.ShouldContain(n => n.Name == "completions");
    [Fact] void should_include_init() => _result.ShouldContain(n => n.Name == "init");
}
