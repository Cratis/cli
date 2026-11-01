// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class after_backup_protection_is_refused : given.a_home_and_a_project
{
    const string Original = "{\"theme\":\"dark\"}\n";
    Exception? _error;

    void Establish()
    {
        Write(ProjectFile(".mcp.json"), Original);
        _plan = DirectMcpRegistration.Install(DirectMcpScope.Project, Locations, ["claude"], DirectMcpClients.Arguments(Origin, null));
    }

    void Because() => _error = Catch.Exception(() => _plan.Apply(AiFileOperations.Performing, (_, _) => throw new IOException("Cannot establish backup ACL protection.")));

    [Fact] void should_refuse_the_apply() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_preserve_the_client_bytes() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(Original);
    [Fact] void should_not_publish_ownership() => File.Exists(ProjectFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
    [Fact] void should_remove_the_partial_backup() => Directory.GetFiles(_project, ".mcp.json.*.bak").ShouldBeEmpty();
}
