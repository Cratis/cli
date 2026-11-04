// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class pi_for_the_user : given.a_home_and_a_project
{
    void Establish()
    {
        _environment["PI_CODING_AGENT_DIR"] = HomeFile("custom-agent");
        Write(HomeFile("custom-agent/mcp.json"), "{\"setting\":true,\"mcpServers\":{\"other\":{\"command\":\"other\"}}}");
    }
    void Because() => _plan = Install(DirectMcpScope.User, ["pi"], "team");
    [Fact] void should_support_pi() => _plan.Unsupported.ShouldBeEmpty();
    [Fact] void should_install_at_the_override() => IsRegistered(HomeFile("custom-agent/mcp.json"), "mcpServers").ShouldBeTrue();
    [Fact] void should_launch_cratis() => ReadJson(HomeFile("custom-agent/mcp.json"))["mcpServers"]["cratis-direct"]["command"].GetValue<string>().ShouldEqual("cratis");
    [Fact] void should_pin_the_target() => ReadJson(HomeFile("custom-agent/mcp.json"))["mcpServers"]["cratis-direct"]["args"].AsArray().Select(value => value.GetValue<string>()).ShouldEqual(["direct", "mcp", "--url", Origin, "--tenant", "team"]);
    [Fact] void should_keep_other_servers() => ReadJson(HomeFile("custom-agent/mcp.json"))["mcpServers"]["other"]["command"].GetValue<string>().ShouldEqual("other");
    [Fact] void should_keep_other_settings() => ReadJson(HomeFile("custom-agent/mcp.json"))["setting"].GetValue<bool>().ShouldBeTrue();
    [Fact] void should_record_ownership() => DirectMcpManifest.Read(_home).Servers.Single().Path.ShouldEqual("custom-agent/mcp.json");
}
