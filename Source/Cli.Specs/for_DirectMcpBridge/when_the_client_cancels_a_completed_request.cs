// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_the_client_cancels_a_completed_request : given.a_bridge
{
    const string Cancel = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/cancelled\",\"params\":{\"requestId\":2}}";
    Exception? _error;

    void Establish()
    {
        // Both responses complete synchronously, so completion precedes the next input line without a timing wait.
        _direct.AnswerWhen("tools/list", _ => Task.FromResult(Json("{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"tools\":[]}}")));
        _direct.AnswerWhen("notifications/cancelled", _ => Task.FromResult(Status(HttpStatusCode.Accepted)));
    }

    async Task Because() => _error = await Catch.Exception(() => Forward(ListTools, Cancel));

    [Fact] void should_forward_the_late_cancellation_to_direct() => _direct.Requests.Select(request => request.Body).ShouldContain(Cancel);
    [Fact] void should_answer_the_completed_request_exactly_once() => OutputLines.Count.ShouldEqual(1);
    [Fact] void should_keep_the_completed_answer() => JsonNode.Parse(OutputLines.Single())!["result"]!["tools"]!.AsArray().Count.ShouldEqual(0);
    [Fact] void should_stop_cleanly_at_eof() => _error.ShouldBeNull();
}
