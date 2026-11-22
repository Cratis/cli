// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

public class with_a_removed_owned_entry : given.a_screenplay_corpus
{
    const string Remaining = """{"mcpServers":{"other":{"command":"keep-me"}}}""";
    JsonNode _installed;
    IReadOnlyList<string> _drift;

    void Establish()
    {
        Install();
        _installed = Read(".mcp.json")["mcpServers"]["screenplay"].DeepClone();
        Write(".mcp.json", Remaining);
        _drift = AiCorpusSynchronizer.Status(_project).ModifiedFiles;
    }

    void Because() => _result = Install();

    [Fact] void should_restore_the_owned_entry() => JsonNode.DeepEquals(Read(".mcp.json")["mcpServers"]["screenplay"], _installed).ShouldBeTrue();
    [Fact] void should_preserve_the_other_server() => Read(".mcp.json")["mcpServers"]["other"]["command"].GetValue<string>().ShouldEqual("keep-me");
    [Fact] void should_report_the_restoration() => _result.Actions.ShouldContain("Restored MCP claude/screenplay in .mcp.json");
    [Fact] void should_not_report_a_conflict() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_still_report_drift_in_status_before_updating() => _drift.Single().ShouldContain("modified owned MCP entry");
}
