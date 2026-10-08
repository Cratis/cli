// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class under_a_search_only_ancestor : given.a_home_and_a_project
{
    Exception? _error;

    void Establish()
    {
        if (!OperatingSystem.IsWindows()) Write(ProjectFile("ancestor/root/.mcp.json"), "{\"theme\":\"dark\"}\n");
    }

    void Because()
    {
        if (OperatingSystem.IsWindows()) return;
        var ancestor = ProjectFile("ancestor");
        var mode = File.GetUnixFileMode(ancestor);
        File.SetUnixFileMode(ancestor, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
        try
        {
            // Remove even owner read access: only search is needed to reach the known configuration.
            File.SetUnixFileMode(ancestor, UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
            _error = Catch.Exception(() => DirectMcpRegistration.Install(DirectMcpScope.Project, Locations with { Project = ProjectFile("ancestor/root") }, ["claude"], DirectMcpClients.Arguments(Origin, null)).Apply(new(false)));
        }
        finally
        {
            File.SetUnixFileMode(ancestor, mode);
        }
    }

    [given.unix_only.Fact] void should_not_require_ancestor_read_permission() => _error.ShouldBeNull();
    [given.unix_only.Fact] void should_rewrite_the_existing_configuration() => IsRegistered(ProjectFile("ancestor/root/.mcp.json"), "mcpServers").ShouldBeTrue();
}
