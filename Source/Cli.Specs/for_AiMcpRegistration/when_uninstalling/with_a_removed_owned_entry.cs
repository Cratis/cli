// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_uninstalling;

public class with_a_removed_owned_entry : given.a_screenplay_corpus
{
    const string Remaining = """{"mcpServers":{"other":{"command":"keep-me"}}}""";

    void Establish()
    {
        Install();
        Write(".mcp.json", Remaining);
    }

    void Because() => _result = AiCorpusSynchronizer.Uninstall(_project);

    [Fact] void should_not_report_a_conflict() => _result.Conflicts.ShouldBeEmpty();
    [Fact] void should_leave_the_file_as_the_user_left_it() => File.ReadAllText(ProjectFile(".mcp.json")).ShouldEqual(Remaining);
}
