// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_playwright_disabled : given.a_playwright_corpus
{
    void Establish() => _configuration = _configuration with { McpServers = new Dictionary<string, AiMcpConfiguration> { ["playwright"] = new(false) } };
    void Because() => _result = Install();

    [Fact] void should_not_register_playwright() => File.Exists(ProjectFile(".mcp.json")).ShouldBeFalse();
    [Fact] void should_not_report_it_as_unsupported() => _result.UnsupportedMcpServers.ShouldBeEmpty();
}
