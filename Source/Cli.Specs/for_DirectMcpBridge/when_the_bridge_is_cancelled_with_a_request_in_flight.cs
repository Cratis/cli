// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_the_bridge_is_cancelled_with_a_request_in_flight : given.requests_in_flight
{
    Exception _error;
    JsonNode _answer;

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        try
        {
            await Forward(
                new Input(
                    async cancellationToken =>
                    {
                        await InFlight();
                        await cancellation.CancelAsync();
                        await Task.Delay(Timeout.Infinite, cancellationToken);
                        return null;
                    },
                    CallTool,
                    Initialized),
                cancellation.Token);
        }
        catch (OperationCanceledException ex)
        {
            _error = ex;
        }

        _answer = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_stop_as_cancelled() => _error.ShouldNotBeNull();
    [Fact] void should_answer_the_request() => _answer["id"]!.GetValue<int>().ShouldEqual(7);
    [Fact] void should_say_the_bridge_stopped() => _answer["error"]!["message"]!.GetValue<string>().ShouldEqual(DirectMcpBridge.BridgeStopped);
}
