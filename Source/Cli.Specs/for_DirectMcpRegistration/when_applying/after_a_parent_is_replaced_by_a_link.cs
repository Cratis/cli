// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class after_a_parent_is_replaced_by_a_link : given.a_home_and_a_project
{
    const string Original = "{\"theme\":\"dark\"}\n";
    Exception? _error;

    void Establish()
    {
        if (OperatingSystem.IsWindows()) return;
        Write(ProjectFile(".cursor/mcp.json"), Original);
        Write(HomeFile("outside/mcp.json"), Original);
        _plan = DirectMcpRegistration.Install(DirectMcpScope.Project, Locations, ["cursor"], DirectMcpClients.Arguments(Origin, null));
    }

    void Because()
    {
        if (OperatingSystem.IsWindows()) return;
        _error = Catch.Exception(() => _plan.Apply(AiFileOperations.Performing with
        {
            BeforeConfigurationOpen = path =>
            {
                var parent = Path.GetDirectoryName(path)!;
                Directory.Move(parent, parent + ".original");
                Directory.CreateSymbolicLink(parent, HomeFile("outside"));
            }
        }));
    }

    [given.unix_only.Fact] void should_write_only_through_the_original_parent_handle() => IsRegistered(ProjectFile(".cursor.original/mcp.json"), "mcpServers").ShouldBeTrue();
    [given.unix_only.Fact] void should_leave_the_link_destination_untouched() => File.ReadAllText(HomeFile("outside/mcp.json")).ShouldEqual(Original);
    [given.unix_only.Fact] void should_complete_the_handle_relative_write() => _error.ShouldBeNull();
}
