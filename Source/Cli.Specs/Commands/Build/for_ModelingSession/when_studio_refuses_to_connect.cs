// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_ModelingSession;

public class when_studio_refuses_to_connect : given.a_modeling_board
{
    Exception _error;

    void Establish() => _connected = new JsonObject { ["connected"] = false, ["outcome"] = "The board does not exist." };

    async Task Because() => _error = await Catch.Exception(() => Run(unattended: false));

    [Fact] void should_refuse() => _error.ShouldBeOfExactType<ModelingSessionRefused>();
    [Fact] void should_say_why() => _error.Message.ShouldContain("The board does not exist.");
    [Fact] void should_not_start_the_agent() => _startedFor.ShouldBeNull();
    [Fact] void should_not_poll() => CallsTo("studio_poll_local_agent").ShouldBeEmpty();
}
