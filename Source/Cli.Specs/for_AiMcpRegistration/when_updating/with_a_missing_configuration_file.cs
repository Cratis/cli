// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

public class with_a_missing_configuration_file : given.a_screenplay_corpus
{
    JsonNode _installed;

    void Establish()
    {
        Install();
        _installed = Read(".vscode/mcp.json")["servers"]["screenplay"].DeepClone();
        Directory.Delete(ProjectFile(".vscode"), recursive: true);
    }

    void Because() => _result = Install();

    [Fact] void should_restore_the_file_and_owned_entry() => JsonNode.DeepEquals(Read(".vscode/mcp.json")["servers"]["screenplay"], _installed).ShouldBeTrue();
    [Fact] void should_report_the_restoration() => _result.Actions.ShouldContain("Restored MCP copilot/screenplay in .vscode/mcp.json");
    [Fact] void should_not_report_a_conflict() => _result.Conflicts.ShouldBeEmpty();
}
