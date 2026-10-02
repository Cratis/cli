// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_direct_fails_with_an_error_that_does_not_answer_the_request : given.a_bridge
{
    JsonNode _response;

    void Establish() => _direct.Answer(() => Json(HttpStatusCode.BadRequest, "{\"jsonrpc\":\"2.0\",\"id\":null,\"error\":{\"code\":-32600,\"message\":\"Bad Request: missing session\"}}"));

    async Task Because()
    {
        await Forward(ListTools);
        _response = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_answer_the_pending_request() => _response["id"]!.GetValue<int>().ShouldEqual(2);
    [Fact] void should_keep_directs_error_code() => _response["error"]!["code"]!.GetValue<int>().ShouldEqual(-32600);
    [Fact] void should_keep_directs_explanation() => _response["error"]!["message"]!.GetValue<string>().ShouldContain("missing session");
    [Fact] void should_report_the_status() => _response["error"]!["message"]!.GetValue<string>().ShouldContain("400");
}
