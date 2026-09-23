// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

public class with_an_unchanged_registration : given.a_screenplay_corpus
{
    string _before;

    void Establish()
    {
        Install();
        _before = File.ReadAllText(ProjectFile(".mcp.json"));
    }

    void Because() => _result = Install();

    [Fact] void should_not_rewrite_the_host_file() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(_before);
    [Fact] void should_not_report_registration_changes() => _result.Actions.Where(action => action.StartsWith("Configured MCP", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_not_report_drift() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
}
