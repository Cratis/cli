// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_screenplay_and_playwright : given.a_playwright_corpus
{
    void Establish() => _configuration = _configuration with { Profiles = ["cratis/stage", "cratis/playwright"] };
    void Because() => _result = Install();

    [Fact] void should_keep_the_screenplay_project_root_arguments() => Args(Read(".mcp.json")["mcpServers"]!["screenplay"]).ShouldEqual("screenplay mcp --project-root-env CLAUDE_PROJECT_DIR");
    [Fact] void should_keep_the_screenplay_cursor_project_root() => Args(Read(".cursor/mcp.json")["mcpServers"]!["screenplay"]).ShouldEqual("screenplay mcp --project-root ${workspaceFolder}");
    [Fact] void should_not_inject_a_project_root_into_playwright() => Args(Read(".mcp.json")["mcpServers"]!["playwright"]).ShouldEqual("-y @playwright/mcp@0.0.83 --headless --isolated");
    [Fact] void should_not_write_env_for_screenplay() => Read(".mcp.json")["mcpServers"]!["screenplay"]!.AsObject().ContainsKey("env").ShouldBeFalse();
    [Fact] void should_report_no_drift() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
}
