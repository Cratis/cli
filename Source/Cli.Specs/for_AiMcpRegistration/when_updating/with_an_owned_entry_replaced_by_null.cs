// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_updating;

public class with_an_owned_entry_replaced_by_null : given.a_screenplay_corpus
{
    const string Modified = """{"mcpServers":{"screenplay":null,"other":{"command":"keep-me"}}}""";

    void Establish()
    {
        Install();
        Write(".mcp.json", Modified);
    }

    void Because() => _result = Install(force: true);

    [Fact] void should_report_a_conflict_even_with_force() => _result.Conflicts.Single().ShouldContain("modified owned MCP entry");
    [Fact] void should_leave_the_existing_entry_unchanged() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(Modified);
    [Fact] void should_not_apply_other_changes() => _result.Actions.ShouldBeEmpty();
}
