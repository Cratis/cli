// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_client_input_cannot_be_routed : given.a_bridge
{
    [Theory]
    [InlineData("{\"id\":1,\"id\":1,\"method\":\"tools/list\"}")]
    [InlineData("[{\"id\":1,\"id\":1,\"method\":\"tools/list\"}]")]
    [InlineData("{\"method\":\"notifications/cancelled\",\"params\":[1]}")]
    [InlineData("{\"method\":\"notifications/cancelled\",\"params\":\"bad\"}")]
    public async Task should_answer_with_a_json_rpc_error_and_continue(string line)
    {
        _direct.Answer(() => Json("{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"tools\":[]}}"));
        await Forward(line);
        await Forward(ListTools);
        OutputLines.Count.ShouldEqual(2);
        JsonNode.Parse(OutputLines[0])!["error"]!["code"]!.GetValue<int>().ShouldEqual(DirectMcpBridge.ParseError);
        JsonNode.Parse(OutputLines[1])!["id"]!.GetValue<int>().ShouldEqual(2);
        _direct.Requests.Count.ShouldEqual(1);
    }
}
