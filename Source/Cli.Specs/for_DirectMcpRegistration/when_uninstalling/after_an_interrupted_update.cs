// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class after_an_interrupted_update : given.an_interrupted_update
{
    void Because() => _plan = Uninstall(DirectMcpScope.User);

    [Fact] void should_not_report_a_conflict() => _plan.Conflicts.ShouldBeEmpty();
    [Fact] void should_remove_the_updated_registration() => IsRegistered(HomeFile(".claude.json"), "mcpServers").ShouldBeFalse();
    [Fact] void should_remove_the_registration_the_update_did_not_reach() => IsRegistered(HomeFile(".cursor/mcp.json"), "mcpServers").ShouldBeFalse();
    [Fact] void should_keep_the_other_server() => ReadJson(HomeFile(".cursor/mcp.json"))["mcpServers"]!["other"].ShouldNotBeNull();
    [Fact] void should_drop_the_ownership_record() => File.Exists(HomeFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
