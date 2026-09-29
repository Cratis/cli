// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_the_client_sends_an_empty_batch : given.a_bridge
{
    JsonNode _response;

    async Task Because()
    {
        await Forward("[]");
        _response = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_answer_without_an_id() => _response["id"].ShouldBeNull();
    [Fact] void should_answer_with_invalid_request() => _response["error"]!["code"]!.GetValue<int>().ShouldEqual(DirectMcpBridge.InvalidRequest);
}
