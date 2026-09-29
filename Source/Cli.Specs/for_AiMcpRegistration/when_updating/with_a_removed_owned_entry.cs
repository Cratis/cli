// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

/// <summary>Removing an owned Screenplay entry is drift like any other change to it; it is not added again automatically.</summary>
public class with_a_removed_owned_entry : given.a_screenplay_corpus
{
    const string Remaining = """{"mcpServers":{"other":{"command":"keep-me"}}}""";

    void Establish()
    {
        Install();
        Write(".mcp.json", Remaining);
    }

    void Because() => _result = Install(force: true);

    [Fact] void should_report_the_removal_as_drift() => _result.Conflicts.Single().ShouldContain("modified owned MCP entry");
    [Fact] void should_not_add_the_entry_again() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(Remaining);
    [Fact] void should_also_report_drift_in_status() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.Single().ShouldContain("modified owned MCP entry");
}
