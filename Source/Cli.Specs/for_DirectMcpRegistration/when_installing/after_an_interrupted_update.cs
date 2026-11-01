// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class after_an_interrupted_update : given.an_interrupted_update
{
    void Because() => _plan = Install(DirectMcpScope.User, ["claude", "cursor"], Tenant);

    [Fact] void should_have_been_interrupted() => _interruption.ShouldNotBeNull();
    [Fact] void should_own_what_it_wrote_before_the_interruption() => _plan.Conflicts.ShouldBeEmpty();
    [Fact] void should_update_only_the_client_that_was_not_written() => _plan.Changes.Select(change => change.Client).ShouldContainOnly("cursor");
    [Fact] void should_pin_the_tenant_for_claude_code() => Launched(HomeFile(".claude.json"), "mcpServers").ShouldContain("\"--tenant\",\"team\"");
    [Fact] void should_pin_the_tenant_for_cursor() => Launched(HomeFile(".cursor/mcp.json"), "mcpServers").ShouldContain("\"--tenant\",\"team\"");
    [Fact] void should_leave_nothing_pending() => File.ReadAllText(HomeFile(DirectMcpManifest.RelativePath)).ShouldNotContain("Pending");
}
