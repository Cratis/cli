// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_foreign_screenplay_entry : given.a_screenplay_corpus
{
    const string Original = """{"mcpServers":{"screenplay":{"command":"my-server"}}}""";

    void Establish() => Write(".mcp.json", Original);
    void Because() => _result = Install(force: true);

    [Fact] void should_report_the_member_conflict() => _result.Conflicts.Single().ShouldContain("user-owned MCP entry");
    [Fact] void should_preserve_the_foreign_entry_even_with_force() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(Original);
    [Fact] void should_not_write_any_corpus_files() => Directory.Exists(ProjectFile(".cratis/ai")).ShouldBeFalse();
    [Fact] void should_not_create_a_model_directory() => Directory.Exists(ProjectFile(".cratis/screenplay")).ShouldBeFalse();
}
