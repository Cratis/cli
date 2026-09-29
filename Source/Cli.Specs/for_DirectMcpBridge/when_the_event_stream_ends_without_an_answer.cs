// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_the_event_stream_ends_without_an_answer : given.a_bridge
{
    const string Progress = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/progress\",\"params\":{\"progress\":1}}";

    void Establish() => _direct.Answer(() => Events("data: " + Progress + "\n\n"));

    Task Because() => Forward(ListTools);

    [Fact] void should_relay_what_direct_sent() => OutputLines[0].ShouldEqual(Progress);
    [Fact] void should_answer_the_request_itself() => JsonNode.Parse(OutputLines[1])!["id"]!.GetValue<int>().ShouldEqual(2);
    [Fact] void should_answer_with_a_transport_error() => JsonNode.Parse(OutputLines[1])!["error"]!["code"]!.GetValue<int>().ShouldEqual(DirectMcpBridge.TransportFailure);
    [Fact] void should_write_nothing_else() => OutputLines.Count.ShouldEqual(2);
}
