// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_dry_run_for_playwright : given.a_playwright_corpus
{
    void Because() => _result = Install(dryRun: true);

    [Fact] void should_not_write_host_configuration() => File.Exists(ProjectFile(".mcp.json")).ShouldBeFalse();
    [Fact] void should_not_write_the_manifest() => File.Exists(ProjectFile(".cratis/ai.manifest.json")).ShouldBeFalse();
    [Fact] void should_report_the_planned_registration() => _result.Actions.ShouldContain(action => action.Contains("MCP claude/playwright"));
}
