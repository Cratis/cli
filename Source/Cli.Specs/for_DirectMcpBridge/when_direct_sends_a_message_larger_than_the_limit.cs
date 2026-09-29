// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_direct_sends_a_message_larger_than_the_limit : given.a_bridge
{
    JsonNode _response;

    void Establish() => _direct.Answer(() => Events("data: {\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"text\":\"" + new string('x', 4096) + "\"}}\n\n"));

    async Task Because()
    {
        await ForwardWithLimit(1024, ListTools);
        _response = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_answer_the_request() => _response["id"]!.GetValue<int>().ShouldEqual(2);
    [Fact] void should_answer_with_a_transport_error() => _response["error"]!["code"]!.GetValue<int>().ShouldEqual(DirectMcpBridge.TransportFailure);
    [Fact] void should_say_the_message_was_too_large() => _response["error"]!["message"]!.GetValue<string>().ShouldContain("larger than the bridge accepts");
}
