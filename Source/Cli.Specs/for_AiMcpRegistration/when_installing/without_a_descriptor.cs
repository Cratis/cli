// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_installing;

public class without_a_descriptor : given.a_screenplay_corpus
{
    void Establish() => File.Delete(Path.Combine(_corpus, ".cratis/ai/mcp-servers.json"));
    void Because() => _result = Install();

    [Fact] void should_install_the_older_corpus_normally() => File.Exists(ProjectFile(".cratis/ai/rules/general.md")).ShouldBeTrue();
    [Fact] void should_not_create_host_configuration() => File.Exists(ProjectFile(".mcp.json")).ShouldBeFalse();
    [Fact] void should_not_report_unsupported_servers_that_were_not_selected() => _result.UnsupportedMcpServers!.ShouldBeEmpty();
    [Fact] void should_not_report_conflicts() => _result.Conflicts.ShouldBeEmpty();
}
