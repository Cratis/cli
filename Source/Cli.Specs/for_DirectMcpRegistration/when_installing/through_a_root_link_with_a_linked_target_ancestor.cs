// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class through_a_root_link_with_a_linked_target_ancestor : given.a_home_and_a_project
{
    Exception? _error;
    string _alias = string.Empty;

    void Establish()
    {
        if (OperatingSystem.IsWindows()) return;
        Write(ProjectFile("root/.mcp.json"), "{\"theme\":\"dark\"}\n");
        Directory.CreateSymbolicLink(HomeFile("ancestor"), _project);
        _alias = HomeFile("selected-root");
        Directory.CreateSymbolicLink(_alias, HomeFile("ancestor/root"));
    }

    void Because()
    {
        if (OperatingSystem.IsWindows()) return;
        _error = Catch.Exception(() => DirectMcpRegistration.Install(DirectMcpScope.Project, Locations with { Project = _alias }, ["claude"], DirectMcpClients.Arguments(Origin, null)).Apply(new(false)));
    }

    [given.unix_only.Fact] void should_resolve_the_entire_selected_scope_root() => _error.ShouldBeNull();
    [given.unix_only.Fact] void should_rewrite_the_existing_configuration() => IsRegistered(ProjectFile("root/.mcp.json"), "mcpServers").ShouldBeTrue();
}
