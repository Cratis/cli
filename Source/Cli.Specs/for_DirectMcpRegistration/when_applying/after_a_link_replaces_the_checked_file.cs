// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class after_a_link_replaces_the_checked_file : given.a_home_and_a_project
{
    const string Original = "{\"theme\":\"dark\"}\n";
    const string Outside = "unrelated private file\n";
    Exception? _error;

    void Establish()
    {
        Write(ProjectFile(".mcp.json"), Original);
        Write(HomeFile("outside.json"), Outside);
        _plan = DirectMcpRegistration.Install(DirectMcpScope.Project, Locations, ["claude"], DirectMcpClients.Arguments(Origin, null));
    }

    void Because()
    {
        _error = Catch.Exception(() => _plan.Apply(AiFileOperations.Performing with
        {
            BeforeConfigurationOpen = path =>
            {
                File.Delete(path);
                File.CreateSymbolicLink(path, HomeFile("outside.json"));
            }
        }));
    }

    [Fact] void should_refuse_the_no_follow_open() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_leave_the_link_destination_untouched() => File.ReadAllText(HomeFile("outside.json")).ShouldEqual(Outside);
    [Fact] void should_not_publish_ownership() => File.Exists(ProjectFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
    [Fact] void should_retain_the_exact_backup() => File.ReadAllText(Directory.GetFiles(_project, ".mcp.json.*.bak").Single()).ShouldEqual(Original);
}
