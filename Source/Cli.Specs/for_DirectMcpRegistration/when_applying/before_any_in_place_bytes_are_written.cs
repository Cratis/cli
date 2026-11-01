// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_applying;

public class before_any_in_place_bytes_are_written : given.a_home_and_a_project
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
        WriteConfigurationContent = (_, _) => throw new IOException("Injected failure before writing.")
    }));

    [Fact] void should_report_that_nothing_was_written() => _error!.Message.Contains("nothing was written", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_recommend_restoring_a_backup() => _error!.Message.Contains("backup", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    [Fact] void should_leave_the_configuration_untouched() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(Original);
}
