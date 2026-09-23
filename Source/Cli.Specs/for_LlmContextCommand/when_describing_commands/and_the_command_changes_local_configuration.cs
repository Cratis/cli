// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.LlmContext;

namespace Cratis.Cli.for_LlmContextCommand.when_describing_commands;

public class and_the_command_changes_local_configuration : given.a_command_catalog
{
    JsonObject _command;

    void Because() => _command = CommandAt(LlmContextCommand.BuildDescriptorJson(), "context set");

    [Fact] void should_describe_the_effect_as_local() => _command["effect"]!.GetValue<string>().ShouldEqual("local");
    [Fact] void should_not_require_confirmation() => _command["requiresConfirmation"]!.GetValue<bool>().ShouldBeFalse();
}
