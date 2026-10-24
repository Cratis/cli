// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_an_invalid_id : given.a_playwright_corpus
{
    void Establish() => File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/mcp-servers.json"), """
        {"schemaVersion":"1.0","servers":[{"id":"bad id","profiles":["cratis/playwright"],"transport":"stdio","command":"x","args":[]},{"id":"playwright","profiles":["cratis/playwright"],"transport":"stdio","command":"npx","args":["-y"]}]}
        """);

    void Because() => _result = Install();

    [Fact] void should_still_register_the_valid_server() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("playwright").ShouldBeTrue();
    [Fact] void should_not_register_the_invalid_id() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("bad id").ShouldBeFalse();
    [Fact] void should_report_why_it_was_skipped() => _result.UnsupportedMcpServers.ShouldContain(message => message.Contains("invalid id"));
}
