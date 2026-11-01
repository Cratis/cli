// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_direct_answers_with_a_body_that_is_not_json : given.a_bridge
{
    JsonNode _response;

    void Establish() => _direct.Answer(() => Json("<html>gateway</html>"));

    async Task Because()
    {
        await Forward(ListTools);
        _response = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_answer_the_request() => _response["id"]!.GetValue<int>().ShouldEqual(2);
    [Fact] void should_answer_with_a_transport_error() => _response["error"]!["code"]!.GetValue<int>().ShouldEqual(DirectMcpBridge.TransportFailure);
}
