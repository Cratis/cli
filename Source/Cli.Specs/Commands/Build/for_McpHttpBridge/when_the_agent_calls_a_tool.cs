// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.Commands.Build.for_McpHttpBridge;

public class when_the_agent_calls_a_tool : given.a_bridged_server
{
    HttpResponseMessage _notified;
    HttpResponseMessage _first;
    HttpResponseMessage _second;
    string _firstAnswer;
    string _secondAnswer;

    async Task Because()
    {
        _notified = await Post("""{"jsonrpc":"2.0","method":"notifications/initialized"}""", _described.BearerToken);
        var first = Post("""{"jsonrpc":"2.0","id":1,"method":"tools/list"}""", _described.BearerToken);
        var second = Post("""{"jsonrpc":"2.0","id":"two","method":"tools/call"}""", _described.BearerToken);
        _first = await first;
        _second = await second;
        _firstAnswer = await _first.Content.ReadAsStringAsync();
        _secondAnswer = await _second.Content.ReadAsStringAsync();
    }

    [Fact] void should_be_reached_over_http_on_this_machine() => _described.Url!.StartsWith("http://127.0.0.1:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_start_a_local_server_in_the_agent() => _described.Command.ShouldBeNull();
    [Fact] void should_keep_the_server_s_name() => _described.Name.ShouldEqual("screenplay");
    [Fact] void should_keep_which_tools_only_read() => _described.ReadOnlyTools.ShouldContainOnly("read");
    [Fact] void should_accept_a_notification_without_an_answer() => _notified.StatusCode.ShouldEqual(HttpStatusCode.Accepted);
    [Fact] void should_answer_a_request_with_its_own_response() => _firstAnswer.ShouldContain("\"id\":1,\"result\":{\"method\":\"tools/list\"}");
    [Fact] void should_answer_each_request_by_its_id() => _secondAnswer.ShouldContain("\"id\":\"two\",\"result\":{\"method\":\"tools/call\"}");
    [Fact] void should_answer_as_json() => _first.Content.Headers.ContentType!.MediaType.ShouldEqual("application/json");
}
