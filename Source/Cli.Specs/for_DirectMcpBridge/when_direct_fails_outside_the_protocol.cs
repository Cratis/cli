// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_direct_fails_outside_the_protocol : given.a_bridge
{
    JsonNode _response;

    void Establish() => _direct.Answer(() => Status(HttpStatusCode.BadGateway));

    async Task Because()
    {
        await Forward(ListTools);
        _response = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_answer_the_request() => _response["id"]!.GetValue<int>().ShouldEqual(2);
    [Fact] void should_answer_with_a_transport_error() => _response["error"]!["code"]!.GetValue<int>().ShouldEqual(DirectMcpBridge.TransportFailure);
    [Fact] void should_report_the_status() => _response["error"]!["message"]!.GetValue<string>().ShouldContain("502");
}
