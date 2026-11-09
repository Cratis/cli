// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Studio.for_StudioFetchCommand;

public class when_studio_has_no_such_context : Specification
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => StudioFetchCommand.Fetch(
        (_, _, _) => Task.FromResult<JsonNode?>(new JsonObject { ["answered"] = false, ["note"] = "There is no slice with the identifier 'x' that this organization can read." }),
        new StudioFetchSettings { Context = "slice", Id = "c1d2e3f4-a5b6-4c7d-8e9f-0a1b2c3d4e5f", Format = "toon" },
        CancellationToken.None));

    [Fact] void should_fail() => _error.ShouldBeOfExactType<StudioMcpError>();
    [Fact] void should_say_why() => _error.Message.ShouldContain("There is no slice");
}
