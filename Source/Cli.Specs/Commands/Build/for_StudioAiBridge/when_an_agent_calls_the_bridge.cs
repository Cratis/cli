// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_StudioAiBridge;

public class when_an_agent_calls_the_bridge : given.a_builder_client
{
    StudioAiBridge _bridge;
    HttpClient _agent;
    HttpStatusCode _unbound;
    HttpStatusCode _unauthorized;
    string _answer;
    string _streamed;

    void Establish()
    {
        _studio.Answer($"work/{given.an_order.SliceId}/ai/chat", HttpStatusCode.OK, """{ "message": { "role": "assistant", "text": "Hello" }, "finishReason": "stop", "usage": { "inputTokens": 3, "outputTokens": 2 } }""");
        _studio.Answer($"work/{given.an_order.SliceId}/ai/chat", HttpStatusCode.OK, """{ "message": { "role": "assistant", "text": "Hello" }, "finishReason": "stop", "usage": { "inputTokens": 3, "outputTokens": 2 } }""");
        _bridge = StudioAiBridge.Start(_client, _ => { });
        _agent = new();
    }

    void Destroy()
    {
        _agent.Dispose();
        _bridge.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    async Task Because()
    {
        const string body = """{ "model": "m", "messages": [ { "role": "user", "content": "Hi" } ] }""";
        _unbound = (await Post(body, authorized: true)).StatusCode;
        _bridge.Bind(given.an_order.SliceId, given.an_order.OrderId);
        _unauthorized = (await Post(body, authorized: false)).StatusCode;
        _answer = await (await Post(body, authorized: true)).Content.ReadAsStringAsync();
        _streamed = await (await Post(body.Replace("\"model\": \"m\"", "\"model\": \"m\", \"stream\": true"), authorized: true)).Content.ReadAsStringAsync();
    }

    async Task<HttpResponseMessage> Post(string body, bool authorized)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_bridge.Url}/v1/chat/completions") { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        if (authorized) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bridge.Token());
        return await _agent.SendAsync(request);
    }

    [Fact] void should_refuse_a_call_when_no_slice_is_being_built() => _unbound.ShouldEqual(HttpStatusCode.Conflict);
    [Fact] void should_refuse_a_caller_without_the_token() => _unauthorized.ShouldEqual(HttpStatusCode.Unauthorized);
    [Fact] void should_answer_in_the_openai_shape() => JsonNode.Parse(_answer)!["choices"]![0]!["message"]!["content"]!.GetValue<string>().ShouldEqual("Hello");
    [Fact] void should_stream_when_asked_to() => _streamed.ShouldContain("data: [DONE]");
    [Fact] void should_send_the_call_under_the_slices_lease() => JsonNode.Parse(_studio.BodiesTo($"work/{given.an_order.SliceId}/ai/chat").First())!["orderId"]!.GetValue<string>().ShouldEqual(given.an_order.OrderId);
    [Fact] void should_point_agents_at_itself() => _bridge.Environment()["OPENAI_BASE_URL"].ShouldEqual($"{_bridge.Url}/v1");
}
