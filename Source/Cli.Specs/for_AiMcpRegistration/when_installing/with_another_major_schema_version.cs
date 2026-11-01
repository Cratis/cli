// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_another_major_schema_version : given.a_playwright_corpus
{
    void Establish() => File.WriteAllText(Path.Combine(_corpus, ".cratis/ai/mcp-servers.json"), """
        {"schemaVersion":"2.0","servers":[{"id":"playwright","profiles":["cratis/playwright"],"transport":"stdio","command":"npx","args":["-y"]}]}
        """);

    void Because() => _result = Install();

    [Fact] void should_not_register_any_server() => File.Exists(ProjectFile(".mcp.json")).ShouldBeFalse();
    [Fact] void should_report_one_message() => _result.UnsupportedMcpServers!.Single().ShouldContain("schemaVersion");
    [Fact] void should_not_report_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
