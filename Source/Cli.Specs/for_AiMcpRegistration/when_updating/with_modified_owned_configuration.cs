// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

public class with_modified_owned_configuration : given.a_screenplay_corpus
{
    string _changed;

    void Establish()
    {
        Install();
        var configuration = Read(".mcp.json");
        configuration["mcpServers"]!["screenplay"]!["command"] = "user-command";
        _changed = configuration.ToJsonString();
        Write(".mcp.json", _changed);
    }

    void Because() => _result = Install(force: true);

    [Fact] void should_report_drift_even_with_force() => _result.Conflicts.Single().ShouldContain("modified owned MCP entry");
    [Fact] void should_preserve_the_modified_entry() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(_changed);
    [Fact] void should_also_report_drift_in_status() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.Single().ShouldContain("modified owned MCP entry");
}
