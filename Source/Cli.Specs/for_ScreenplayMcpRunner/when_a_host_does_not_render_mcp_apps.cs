// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpRunner;

public class when_a_host_does_not_render_mcp_apps : given.an_embedded_connection
{
    void Because() => Exchange("{}");

    [Fact] void should_return_only_the_two_protocol_responses() => _responses.Length.ShouldEqual(2);
    [Fact] void should_not_offer_visualize_model() => _responses[1].GetProperty("result").GetProperty("tools").EnumerateArray().Any(tool => tool.GetProperty("name").GetString() == "visualize-model").ShouldBeFalse();
    [Fact] void should_keep_the_authoring_tools_available() => _responses[1].GetProperty("result").GetProperty("tools").EnumerateArray().Any(tool => tool.GetProperty("name").GetString() == "read-ast").ShouldBeTrue();
}
