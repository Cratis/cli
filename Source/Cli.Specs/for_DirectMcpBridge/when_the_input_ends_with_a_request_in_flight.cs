// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_the_input_ends_with_a_request_in_flight : given.requests_in_flight
{
    JsonNode _answer;

    async Task Because()
    {
        await Forward(new Input(
            async _ =>
            {
                await InFlight();
                return null;
            },
            CallTool,
            Initialized));
        _answer = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_answer_the_request() => _answer["id"]!.GetValue<int>().ShouldEqual(7);
    [Fact] void should_answer_with_a_transport_error() => _answer["error"]!["code"]!.GetValue<int>().ShouldEqual(DirectMcpBridge.TransportFailure);
    [Fact] void should_say_the_bridge_stopped() => _answer["error"]!["message"]!.GetValue<string>().ShouldEqual(DirectMcpBridge.BridgeStopped);
}
