// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class after_an_in_place_write_fails : given.a_home_and_a_project
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
        WriteConfigurationContent = (stream, _) =>
        {
            stream.WriteByte(0);
            stream.Flush();
            throw new IOException("Injected disk-full failure.");
        }
    }));

    [Fact] void should_report_an_io_failure() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_name_the_exact_backup() => _error!.Message.Contains(Directory.GetFiles(AiProjectPaths.PhysicalRoot(_project), ".mcp.json.*.bak").Single(), StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_damaged_file() => _error!.Message.Contains(Path.Combine(AiProjectPaths.PhysicalRoot(_project), ".mcp.json"), StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_explain_content_only_restoration() => _error!.Message.Contains("into the existing file", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_retain_the_original_backup_bytes() => File.ReadAllText(Directory.GetFiles(_project, ".mcp.json.*.bak").Single()).ShouldEqual(Original);
    [Fact] void should_not_publish_ownership() => File.Exists(ProjectFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
