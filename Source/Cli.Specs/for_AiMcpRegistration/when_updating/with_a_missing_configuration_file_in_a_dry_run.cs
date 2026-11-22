// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

public class with_a_missing_configuration_file_in_a_dry_run : given.a_screenplay_corpus
{
    void Establish()
    {
        Install();
        File.Delete(ProjectFile(".vscode/mcp.json"));
    }

    void Because() => _result = Install(dryRun: true);

    [Fact] void should_report_the_proposed_restoration() => _result.Actions.ShouldContain("Restored MCP copilot/screenplay in .vscode/mcp.json");
    [Fact] void should_not_write_the_file() => File.Exists(ProjectFile(".vscode/mcp.json")).ShouldBeFalse();
    [Fact] void should_not_report_a_conflict() => _result.Conflicts.ShouldBeEmpty();
}
