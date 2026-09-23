// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_dry_run : given.a_screenplay_corpus
{
    void Because() => _result = Install(dryRun: true);

    [Fact] void should_report_registration_actions() => _result.Actions.Count(action => action.StartsWith("Configured MCP", StringComparison.Ordinal)).ShouldEqual(6);
    [Fact] void should_leave_the_entire_project_empty() => Directory.GetFileSystemEntries(_project).ShouldBeEmpty();
}
