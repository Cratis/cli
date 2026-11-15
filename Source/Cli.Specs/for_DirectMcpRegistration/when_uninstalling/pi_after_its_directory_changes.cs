// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class pi_after_its_directory_changes : given.a_home_and_a_project
{
    DirectMcpClientStatus _status;
    void Establish()
    {
        _environment["PI_CODING_AGENT_DIR"] = HomeFile("previous-agent");
        Write(HomeFile("previous-agent/mcp.json"), "{\"mcpServers\":{\"other\":{\"command\":\"other\"}}}");
        Install(DirectMcpScope.User, ["pi"]);
        _environment["PI_CODING_AGENT_DIR"] = Path.Combine(_home, "..", "external-agent");
        _status = DirectMcpRegistration.Status(DirectMcpScope.User, Locations, ["pi"]).Single();
    }
    void Because() => _plan = Uninstall(DirectMcpScope.User, "pi");
    [Fact] void should_report_historical_ownership() => _status.State.ShouldEqual("registered");
    [Fact] void should_remove_only_the_owned_member() => IsRegistered(HomeFile("previous-agent/mcp.json"), "mcpServers").ShouldBeFalse();
    [Fact] void should_keep_other_servers() => ReadJson(HomeFile("previous-agent/mcp.json"))["mcpServers"]["other"]["command"].GetValue<string>().ShouldEqual("other");
    [Fact] void should_forget_ownership() => File.Exists(HomeFile(DirectMcpManifest.RelativePath)).ShouldBeFalse();
}
