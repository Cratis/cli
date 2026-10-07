// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class after_an_identical_file_replaces_the_checked_inode : given.a_home_and_a_project
{
    const string Original = "{\"theme\":\"dark\"}\n";
    Exception? _error;

    void Establish()
    {
        Write(ProjectFile(".mcp.json"), Original);
        _plan = DirectMcpRegistration.Install(DirectMcpScope.Project, Locations, ["claude"], DirectMcpClients.Arguments(Origin, null));
    }

    void Because() => _error = Catch.Exception(() => _plan.Apply(AiFileOperations.Performing with
    {
        BeforeConfigurationOpen = path =>
        {
            File.Move(path, path + ".old");
            File.WriteAllText(path, Original);
        }
    }));

    [Fact] void should_refuse_a_different_inode() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_leave_the_new_file_untouched() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(Original);
    [Fact] void should_leave_the_original_file_untouched() => File.ReadAllText(ProjectFile(".mcp.json.old")).ShouldEqual(Original);
    [Fact] void should_not_publish_ownership() => File.Exists(ProjectFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
