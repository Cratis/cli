// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_minor_schema_version : given.a_playwright_corpus
{
    void Establish() => File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/mcp-servers.json"), """
        {"schemaVersion":"1.1","futureField":true,"servers":[{"id":"playwright","profiles":["cratis/playwright"],"transport":"stdio","command":"npx","args":["-y"],"extra":1}]}
        """);

    void Because() => _result = Install();

    [Fact] void should_register_the_server() => Read(".mcp.json")["mcpServers"]!.AsObject().ContainsKey("playwright").ShouldBeTrue();
}
