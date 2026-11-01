// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_direct_answers_with_an_error_without_an_id : given.a_bridge
{
    JsonNode _response;

    void Establish() => _direct.Answer(() => Json("{\"jsonrpc\":\"2.0\",\"id\":null,\"error\":{\"code\":-32603,\"message\":\"Internal error\"}}"));

    async Task Because()
    {
        await Forward(ListTools);
        _response = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_answer_the_pending_request_instead() => _response["id"]!.GetValue<int>().ShouldEqual(2);
    [Fact] void should_keep_directs_error() => _response["error"]!["code"]!.GetValue<int>().ShouldEqual(-32603);
}
