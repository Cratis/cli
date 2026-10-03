// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpRunner;

public class when_a_host_renders_mcp_apps : given.an_embedded_connection
{
    void Because() => Exchange(
        "{\"extensions\":{\"io.modelcontextprotocol/ui\":{\"mimeTypes\":[\"text/html;profile=mcp-app\"]}}}",
        "{\"jsonrpc\":\"2.0\",\"id\":3,\"method\":\"resources/read\",\"params\":{\"uri\":\"ui://screenplay/event-model-board.html\"}}");

    [Fact] void should_return_only_the_three_protocol_responses() => _responses.Length.ShouldEqual(3);
    [Fact] void should_negotiate_the_ui_extension() => _responses[0].GetProperty("result").GetProperty("capabilities").GetProperty("extensions").TryGetProperty("io.modelcontextprotocol/ui", out _).ShouldBeTrue();
    [Fact] void should_offer_visualize_model() => _responses[1].GetProperty("result").GetProperty("tools").EnumerateArray().Any(tool => tool.GetProperty("name").GetString() == "visualize-model").ShouldBeTrue();
    [Fact] void should_bundle_the_board_html() => _responses[2].GetProperty("result").GetProperty("contents")[0].GetProperty("text").GetString()!.ShouldContain("<!doctype html>", StringComparison.OrdinalIgnoreCase);
    [Fact] void should_serve_the_mcp_app_mime_type() => _responses[2].GetProperty("result").GetProperty("contents")[0].GetProperty("mimeType").GetString().ShouldEqual("text/html;profile=mcp-app");
}
