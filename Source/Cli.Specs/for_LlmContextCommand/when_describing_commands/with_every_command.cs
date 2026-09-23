// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Cli.Commands.LlmContext;

namespace Cratis.Cli.for_LlmContextCommand.when_describing_commands;

public class with_every_command : given.a_command_catalog
{
    IDictionary<string, JsonObject> _commands;

    void Because() => _commands = AllCommands(LlmContextCommand.BuildDescriptorJson());

    [Fact] void should_describe_commands() => _commands.ShouldNotBeEmpty();
    [Fact] void should_give_every_command_a_known_effect() => _commands.Where(_ => !_effects.Contains(_.Value["effect"]?.GetValue<string>() ?? string.Empty)).Select(_ => _.Key).ShouldBeEmpty();
    [Fact] void should_state_for_every_command_whether_it_requires_confirmation() => _commands.Where(_ => _.Value["requiresConfirmation"]?.GetValueKind() is not (JsonValueKind.True or JsonValueKind.False)).Select(_ => _.Key).ShouldBeEmpty();
}
