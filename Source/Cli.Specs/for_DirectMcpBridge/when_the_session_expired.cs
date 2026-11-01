// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_the_session_expired : given.a_bridge
{
    JsonNode _response;

    void Establish()
    {
        _direct.Answer(() =>
        {
            var response = Json("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"protocolVersion\":\"2025-06-18\"}}");
            response.Headers.Add("Mcp-Session-Id", "first-session");
            return response;
        });
        _direct.Answer(() => Json(HttpStatusCode.NotFound, "{\"jsonrpc\":\"2.0\",\"id\":null,\"error\":{\"code\":-32001,\"message\":\"Session not found\"}}"));
        _direct.Answer(() => Json("{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"protocolVersion\":\"2025-06-18\"}}"));
    }

    /// <summary>Forwards sequentially, as a client re-initializes only after it saw the failure.</summary>
    /// <returns>A task.</returns>
    async Task Because()
    {
        await Forward(Initialize);
        await Forward(ListTools);
        _response = JsonNode.Parse(OutputLines[1])!;
        await Forward(Initialize);
    }

    [Fact] void should_send_the_session_it_was_given() => _direct.Requests[1].SessionId.ShouldEqual("first-session");
    [Fact] void should_answer_the_request() => _response["id"]!.GetValue<int>().ShouldEqual(2);
    [Fact] void should_explain_that_the_session_ended() => _response["error"]!["message"]!.GetValue<string>().ShouldEqual(DirectMcpBridge.SessionEnded);
    [Fact] void should_initialize_again_without_the_dead_session() => _direct.Requests[2].SessionId.ShouldBeNull();
    [Fact] void should_not_send_a_protocol_version_with_initialize() => _direct.Requests[2].ProtocolVersion.ShouldBeNull();
}
