// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_reading_the_input_fails_with_a_request_in_flight : given.requests_in_flight
{
    Exception _error;
    JsonNode _answer;

    async Task Because()
    {
        try
        {
            await Forward(new Input(
                async _ =>
                {
                    await InFlight();
                    throw new IOException("The pipe broke.");
                },
                CallTool,
                Initialized));
        }
        catch (IOException ex)
        {
            _error = ex;
        }

        _answer = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_fail_with_the_read_error() => _error.Message.ShouldEqual("The pipe broke.");
    [Fact] void should_answer_the_request() => _answer["id"]!.GetValue<int>().ShouldEqual(7);
    [Fact] void should_say_the_bridge_stopped() => _answer["error"]!["message"]!.GetValue<string>().ShouldEqual(DirectMcpBridge.BridgeStopped);
}
