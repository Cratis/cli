// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_an_incomplete_descriptor : given.a_playwright_corpus
{
    void Establish() => File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/mcp-servers.json"), """
        {"schemaVersion":"1.0","servers":[{"id":"bad","profiles":["cratis/playwright"],"transport":"stdio","args":[]},{"id":"playwright","profiles":["cratis/playwright"],"transport":"stdio","command":"npx","args":["-y"]}]}
        """);

    void Because() => _result = Install();

    [Fact] void should_skip_the_server() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("bad").ShouldBeFalse();
    [Fact] void should_still_register_the_valid_server() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("playwright").ShouldBeTrue();
    [Fact] void should_report_why_it_was_skipped() => _result.UnsupportedMcpServers.ShouldContain(message => message.Contains("incomplete descriptor"));
}
