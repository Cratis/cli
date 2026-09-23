// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class with_a_foreign_codex_entry : given.a_screenplay_corpus
{
    const string Original = "[mcp_servers.'screenplay']\ncommand = 'my-server' # keep\n";

    void Establish() => Write(".codex/config.toml", Original);
    void Because() => _result = Install(force: true);

    [Fact] void should_report_a_native_table_conflict() => _result.Conflicts.Single().ShouldContain("user-owned MCP entry");
    [Fact] void should_preserve_the_foreign_table() => File.ReadAllText(ProjectFile(".codex/config.toml")).ShouldEqual(Original);
    [Fact] void should_write_nothing_else() => File.Exists(ProjectFile(".mcp.json")).ShouldBeFalse();
}
