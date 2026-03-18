// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CompletionsCommand;

public class when_getting_command_tree : Specification
{
    IReadOnlyList<CommandNode> _result;

    void Because() => _result = CliCommandTree.Commands;

    [Fact] void should_include_chronicle() => _result.ShouldContain(n => n.Name == "chronicle");
    [Fact] void should_include_context() => _result.ShouldContain(n => n.Name == "context");
    [Fact] void should_include_chat() => _result.ShouldContain(n => n.Name == "chat");
    [Fact] void should_include_completions() => _result.ShouldContain(n => n.Name == "completions");
    [Fact] void should_include_init() => _result.ShouldContain(n => n.Name == "init");
}
