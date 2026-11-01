// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class after_an_update_backup_is_refused : given.a_home_and_a_project
{
    byte[] _client;
    byte[] _manifest;
    Exception? _error;

    void Establish()
    {
        Install(DirectMcpScope.Project, ["claude"]);
        _client = File.ReadAllBytes(ProjectFile(".mcp.json"));
        _manifest = File.ReadAllBytes(ProjectFile(DirectMcpManifest.RelativePath));
        _plan = DirectMcpRegistration.Install(DirectMcpScope.Project, Locations, ["claude"], DirectMcpClients.Arguments(Origin, "team"));
    }

    void Because() => _error = Catch.Exception(() => _plan.Apply(AiFileOperations.Performing, (_, _) => throw new IOException("Cannot establish backup ACL protection.")));

    [Fact] void should_refuse_the_update() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_preserve_the_client_bytes() => File.ReadAllBytes(ProjectFile(".mcp.json")).ShouldEqual(_client);
    [Fact] void should_preserve_the_exact_ownership_record() => File.ReadAllBytes(ProjectFile(DirectMcpManifest.RelativePath)).ShouldEqual(_manifest);
    [Fact] void should_remove_the_partial_backup() => Directory.GetFiles(_project, ".mcp.json.*.bak").ShouldBeEmpty();
}
