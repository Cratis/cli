// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class with_a_pinned_tenant_over_an_owned_registration : given.a_home_and_a_project
{
    void Establish() => Install(DirectMcpScope.User, ["cursor"]);

    void Because() => _plan = Install(DirectMcpScope.User, ["cursor"], tenant: "team");

    [Fact] void should_update_the_registration() => _plan.Changes.Single().Action.ShouldEqual("update");
    [Fact] void should_pin_the_tenant() => ReadJson(HomeFile(".cursor/mcp.json"))["mcpServers"]!["cratis-direct"]!["args"]!.ToJsonString().ShouldEqual("[\"direct\",\"mcp\",\"--tenant\",\"team\"]");
    [Fact] void should_record_the_new_value_as_owned() => DirectMcpManifest.Read(_home).Servers.Single().Installed["args"]!.AsArray().Count.ShouldEqual(4);
}
