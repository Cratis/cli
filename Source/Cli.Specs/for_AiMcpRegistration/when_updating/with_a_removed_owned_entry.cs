// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

public class with_a_removed_owned_entry : given.a_screenplay_corpus
{
    IReadOnlyList<string> _drift;

    void Establish()
    {
        Install();
        Write(".mcp.json", """{"mcpServers":{"other":{"command":"keep-me"}}}""");
        _drift = AiCorpusSynchronizer.Status(_project).ModifiedFiles;
    }

    void Because() => _result = Install();

    [Fact] void should_report_the_removal_in_status() => _drift.ShouldContain(entry => entry.Contains("owned MCP entry removed"));
    [Fact] void should_not_report_a_conflict() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_add_the_entry_again() => Read(".mcp.json")["mcpServers"]!["screenplay"]!["command"]!.GetValue<string>().ShouldEqual("cratis");
    [Fact] void should_keep_the_other_server() => Read(".mcp.json")["mcpServers"]!["other"]!["command"]!.GetValue<string>().ShouldEqual("keep-me");
    [Fact] void should_no_longer_report_drift() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
}
