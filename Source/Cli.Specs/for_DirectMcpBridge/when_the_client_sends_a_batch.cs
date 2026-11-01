// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_the_client_sends_a_batch : given.a_bridge
{
    IReadOnlyList<JsonNode> _responses;

    async Task Because()
    {
        await Forward($"[{ListTools},{Initialized},{{\"jsonrpc\":\"2.0\",\"id\":\"ping-1\",\"method\":\"ping\"}}]");
        _responses = [.. OutputLines.Select(line => JsonNode.Parse(line)!)];
    }

    [Fact] void should_not_call_direct() => _direct.Requests.ShouldBeEmpty();
    [Fact] void should_answer_each_request_but_not_the_notification() => _responses.Select(response => response["id"]!.ToJsonString()).ShouldContainOnly("2", "\"ping-1\"");
    [Fact] void should_answer_with_invalid_request() => _responses.Select(response => response["error"]!["code"]!.GetValue<int>()).ShouldContainOnly(DirectMcpBridge.InvalidRequest, DirectMcpBridge.InvalidRequest);
}
