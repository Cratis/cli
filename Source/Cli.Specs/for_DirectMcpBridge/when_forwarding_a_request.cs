// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectMcpBridge;

public class when_forwarding_a_request : given.a_bridge
{
    const string Response = "{\"jsonrpc\":\"2.0\",\"id\":2,\"result\":{\"tools\":[]}}";

    void Establish() => _direct.Answer(() => Json("{\n  \"jsonrpc\": \"2.0\",\n  \"id\": 2,\n  \"result\": { \"tools\": [] }\n}"));

    Task Because() => Forward(ListTools);

    [Fact] void should_post_to_the_pinned_resource() => _direct.Requests.Single().Uri.ShouldEqual(new Uri("https://direct.example/mcp"));
    [Fact] void should_post() => _direct.Requests.Single().Method.ShouldEqual(HttpMethod.Post);
    [Fact] void should_forward_the_message_unchanged() => _direct.Requests.Single().Body.ShouldEqual(ListTools);
    [Fact] void should_send_the_access_token_as_bearer() => _direct.Requests.Single().Authorization.ShouldEqual($"Bearer {FirstToken}");
    [Fact] void should_accept_json_and_event_streams() => _direct.Requests.Single().Accept.ShouldEqual("application/json, text/event-stream");
    [Fact] void should_send_json() => _direct.Requests.Single().ContentType.ShouldEqual("application/json");
    [Fact] void should_write_the_response_as_one_line() => _output.ToString().ShouldEqual(Response + "\n");
    [Fact] void should_not_log_the_token() => _log.ToString().ShouldNotContain(FirstToken);
}
