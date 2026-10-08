// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpRunner;

public class when_listing_bundled_authoring_tools : given.an_embedded_connection
{
    string _workspaceDescription;
    string[] _tools;
    string _instructions;

    void Because()
    {
        Exchange("{}");
        _tools = [.. _responses[1].GetProperty("result").GetProperty("tools").EnumerateArray().Select(tool => tool.GetProperty("name").GetString())];
        _instructions = _responses[0].GetProperty("result").GetProperty("instructions").GetString();
        _workspaceDescription = _responses[1].GetProperty("result").GetProperty("tools").EnumerateArray()
            .Single(tool => tool.GetProperty("name").GetString() == "read-workspace").GetProperty("description").GetString();
    }

    [Fact] void should_offer_whole_source_proposals() => _tools.ShouldContain("propose-source");
    [Fact] void should_retain_typed_ast_proposals() => _tools.ShouldContain("propose-ast");
    [Fact] void should_explain_whole_source_authoring_to_clients() => _instructions.ShouldContain("propose-source");
    [Fact] void should_offer_event_source_views() => _workspaceDescription.ShouldContain("event-sources");
    [Fact] void should_offer_event_stream_views() => _workspaceDescription.ShouldContain("event-streams");
    [Fact] void should_offer_command_route_views() => _workspaceDescription.ShouldContain("command-routes");
}
