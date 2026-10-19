// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_duplicate_id : given.a_playwright_corpus
{
    void Establish() => File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/mcp-servers.json"), """
        {"schemaVersion":"1.0","servers":[{"id":"dup","profiles":["cratis/playwright"],"transport":"stdio","command":"x","args":[]},{"id":"dup","profiles":["cratis/playwright"],"transport":"stdio","command":"y","args":[]},{"id":"playwright","profiles":["cratis/playwright"],"transport":"stdio","command":"npx","args":["-y"]}]}
        """);

    void Because() => _result = Install();

    [Fact] void should_skip_every_entry_with_the_id() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("dup").ShouldBeFalse();
    [Fact] void should_still_register_the_valid_server() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("playwright").ShouldBeTrue();
    [Fact] void should_report_each_skipped_entry() => _result.UnsupportedMcpServers!.Count(message => message.Contains("duplicate id")).ShouldEqual(2);
}
