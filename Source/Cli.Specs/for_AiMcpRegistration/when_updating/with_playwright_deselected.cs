// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

public class with_playwright_deselected : given.a_playwright_corpus
{
    void Establish()
    {
        Install();
        _configuration = _configuration with { Profiles = ["cratis/stage"] };
    }

    void Because() => _result = Install();

    [Fact] void should_remove_it_from_claude() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("playwright").ShouldBeFalse();
    [Fact] void should_remove_it_from_opencode() => Read("opencode.json")["mcp"]!.AsObject().ContainsKey("playwright").ShouldBeFalse();
    [Fact] void should_remove_it_from_codex() => File.ReadAllText(ProjectFile(".codex/config.toml")).ShouldNotContain("playwright");
    [Fact] void should_keep_the_selected_screenplay_registration() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("screenplay").ShouldBeTrue();
    [Fact] void should_report_no_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
