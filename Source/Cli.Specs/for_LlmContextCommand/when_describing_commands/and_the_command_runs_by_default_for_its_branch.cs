// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.LlmContext;

namespace Cratis.Cli.for_LlmContextCommand.when_describing_commands;

public class and_the_command_runs_by_default_for_its_branch : given.a_command_catalog
{
    IDictionary<string, JsonObject> _commands;

    void Because() => _commands = AllCommands(LlmContextCommand.BuildDescriptorJson());

    [Fact] void should_describe_it_by_the_branch_path() => _commands["direct mcp"]["effect"]!.GetValue<string>().ShouldEqual("mutating");
    [Fact] void should_not_describe_it_as_a_child_of_its_branch() => _commands.ContainsKey("direct mcp mcp").ShouldBeFalse();
}
