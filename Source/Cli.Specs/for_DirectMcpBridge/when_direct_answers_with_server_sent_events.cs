// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_direct_answers_with_server_sent_events : given.a_bridge
{
    const string Progress = "{\"jsonrpc\":\"2.0\",\"method\":\"notifications/progress\",\"params\":{\"progress\":1}}";
    const string Response = "{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"tools\":[]}}";

    void Establish() => _direct.Answer(() => Events(
        ": keep-alive\n\n" +
        "event: message\nid: 1\ndata: " + Progress + "\n\n" +
        "event: other\ndata: {\"ignored\":true}\n\n" +
        "data: {\"jsonrpc\":\"2.0\",\n" +
        "data: \"id\":2,\"result\":{\"tools\":[]}}\n\n"));

    Task Because() => Forward(ListTools);

    [Fact] void should_relay_every_message_event_in_order() => OutputLines.ShouldEqual(Progress, Response);
}
