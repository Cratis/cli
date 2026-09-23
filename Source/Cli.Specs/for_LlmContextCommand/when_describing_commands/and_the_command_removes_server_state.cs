// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.LlmContext;

namespace Cratis.Cli.for_LlmContextCommand.when_describing_commands;

public class and_the_command_removes_server_state : given.a_command_catalog
{
    JsonObject _command;

    void Because() => _command = CommandAt(LlmContextCommand.BuildDescriptorJson(), "chronicle users remove");

    [Fact] void should_describe_the_effect_as_destructive() => _command["effect"]!.GetValue<string>().ShouldEqual("destructive");
    [Fact] void should_require_confirmation() => _command["requiresConfirmation"]!.GetValue<bool>().ShouldBeTrue();
}
