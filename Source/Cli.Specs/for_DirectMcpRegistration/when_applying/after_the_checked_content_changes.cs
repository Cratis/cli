// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class after_the_checked_content_changes : given.a_home_and_a_project
{
    const string Changed = "{\"theme\":\"light\"}\n";
    Exception? _error;

    void Establish()
    {
        Write(ProjectFile(".mcp.json"), "{\"theme\":\"dark\"}\n");
        _plan = DirectMcpRegistration.Install(DirectMcpScope.Project, Locations, ["claude"], DirectMcpClients.Arguments(Origin, null));
    }

    void Because() => _error = Catch.Exception(() => _plan.Apply(AiFileOperations.Performing with { BeforeConfigurationOpen = path => File.WriteAllText(path, Changed) }));

    [Fact] void should_refuse_changed_bytes_on_the_opened_handle() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_report_that_nothing_was_written() => _error!.Message.Contains("nothing was written", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_recommend_overwriting_the_concurrent_edit() => _error!.Message.Contains("backup", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    [Fact] void should_leave_the_changed_content_untouched() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(Changed);
    [Fact] void should_not_publish_ownership() => File.Exists(ProjectFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
