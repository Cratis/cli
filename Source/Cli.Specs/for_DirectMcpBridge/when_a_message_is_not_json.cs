// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_a_message_is_not_json : given.a_bridge
{
    JsonNode _response;

    async Task Because()
    {
        await Forward("not json");
        _response = JsonNode.Parse(OutputLines.Single())!;
    }

    [Fact] void should_not_call_direct() => _direct.Requests.ShouldBeEmpty();
    [Fact] void should_answer_with_a_parse_error() => _response["error"]!["code"]!.GetValue<int>().ShouldEqual(DirectMcpBridge.ParseError);
}
