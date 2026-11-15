// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class pi_for_a_project : given.a_home_and_a_project
{
    void Establish() => _environment["PI_CODING_AGENT_DIR"] = Path.Combine(_home, "..", "external-agent");
    void Because() => _plan = Install(DirectMcpScope.Project, ["pi"]);
    [Fact] void should_use_the_project_path() => IsRegistered(ProjectFile(".pi/mcp.json"), "mcpServers").ShouldBeTrue();
    [Fact] void should_not_write_user_configuration() => File.Exists(HomeFile(".pi/agent/mcp.json")).ShouldBeFalse();
    [Fact] void should_pin_no_tenant() => ReadJson(ProjectFile(".pi/mcp.json"))["mcpServers"]["cratis-direct"]["args"].AsArray()[^1].GetValue<string>().ShouldEqual("--no-tenant");
}
