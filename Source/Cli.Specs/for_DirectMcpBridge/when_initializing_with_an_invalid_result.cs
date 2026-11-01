// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_initializing_with_an_invalid_result : given.a_bridge
{
    [Theory]
    [InlineData("42")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{\"protocolVersion\":42}")]
    public async Task should_answer_once_with_a_correlated_bridge_error(string result)
    {
        _direct.Answer(() => Json($"{{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{result}}}"));
        await Forward(Initialize);
        var response = JsonNode.Parse(OutputLines.Single())!;
        response["id"]!.GetValue<int>().ShouldEqual(1);
        response["error"]!["code"]!.GetValue<int>().ShouldEqual(DirectMcpBridge.TransportFailure);
    }
}
