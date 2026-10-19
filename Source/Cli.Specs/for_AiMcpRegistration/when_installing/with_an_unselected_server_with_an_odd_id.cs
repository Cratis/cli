// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_an_unselected_server_with_an_odd_id : given.a_playwright_corpus
{
    void Establish() => File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/mcp-servers.json"), """
        {"schemaVersion":"1.0","servers":[{"id":"odd id","profiles":["cratis/elsewhere"],"transport":"stdio","command":"x","args":[]},{"id":"playwright","profiles":["cratis/playwright"],"transport":"stdio","command":"npx","args":["-y"]}]}
        """);

    void Because() => _result = Install();

    [Fact] void should_still_register_the_selected_server() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("playwright").ShouldBeTrue();
    [Fact] void should_not_register_the_odd_one() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("odd id").ShouldBeFalse();
    [Fact] void should_report_no_drift() => AiCorpusSynchronizer.Status(_project).ModifiedFiles.ShouldBeEmpty();
}
