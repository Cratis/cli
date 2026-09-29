// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_forwarding_fails_unexpectedly : given.a_bridge
{
    JsonNode _answer;

    void Establish() => _tokens.Failure = new InvalidOperationException("The credential store is corrupt.");

    async Task Because()
    {
        await Forward(ListTools, Initialized);
        _answer = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_answer_the_request() => _answer["id"]!.GetValue<int>().ShouldEqual(2);
    [Fact] void should_answer_with_an_internal_error() => _answer["error"]!["code"]!.GetValue<int>().ShouldEqual(DirectMcpBridge.InternalError);
    [Fact] void should_point_to_the_log() => _answer["error"]!["message"]!.GetValue<string>().ShouldEqual(DirectMcpBridge.ForwardingFailed);
    [Fact] void should_log_what_failed() => _log.ToString().ShouldContain("The credential store is corrupt.");
}
