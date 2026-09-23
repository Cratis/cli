// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiMcpRegistration.when_uninstalling;

public class with_a_modified_owned_entry : given.a_screenplay_corpus
{
    void Establish()
    {
        Install();
        Write(".mcp.json", """{"mcpServers":{"screenplay":{"command":"changed"}}}""");
    }

    void Because() => _result = AiCorpusSynchronizer.Uninstall(_project, force: true);

    [Fact] void should_not_delete_the_changed_entry() => Read(".mcp.json")["mcpServers"]!["screenplay"]!["command"]!.GetValue<string>().ShouldEqual("changed");
    [Fact] void should_report_the_conflict() => _result.Conflicts.ShouldNotBeEmpty();
    [Fact] void should_keep_the_manifest_for_recovery() => File.Exists(ProjectFile(".cratis/ai.manifest.json")).ShouldBeTrue();
}
