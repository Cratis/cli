// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpRunner;

public class when_listing_bundled_authoring_tools : given.an_embedded_connection
{
    string _workspaceDescription;

    void Because()
    {
        Exchange("{}");
        _workspaceDescription = _responses[1].GetProperty("result").GetProperty("tools").EnumerateArray()
            .Single(tool => tool.GetProperty("name").GetString() == "read-workspace").GetProperty("description").GetString()!;
    }

    [Fact] void should_offer_event_source_views() => _workspaceDescription.ShouldContain("event-sources");
    [Fact] void should_offer_event_stream_views() => _workspaceDescription.ShouldContain("event-streams");
    [Fact] void should_offer_command_route_views() => _workspaceDescription.ShouldContain("command-routes");
}
