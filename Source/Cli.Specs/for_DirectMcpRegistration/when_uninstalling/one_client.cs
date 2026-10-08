// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class one_client : given.a_home_and_a_project
{
    void Establish() => Install(DirectMcpScope.User, ["claude", "cursor"]);

    void Because() => _plan = Uninstall(DirectMcpScope.User, "cursor");

    [Fact] void should_remove_that_client() => ReadJson(HomeFile(".cursor/mcp.json"))["mcpServers"]!.AsObject().ContainsKey("cratis-direct").ShouldBeFalse();
    [Fact] void should_keep_the_other_registration() => ReadJson(HomeFile(".claude.json"))["mcpServers"]!["cratis-direct"].ShouldNotBeNull();
    [Fact] void should_keep_owning_the_other_registration() => DirectMcpManifest.Read(_home).Servers.Single().Harness.ShouldEqual("claude");
}
