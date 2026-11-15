// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_installing;

public class pi_with_a_relative_or_tilde_directory : given.a_home_and_a_project
{
    [Theory]
    [InlineData("~/.agents/pi", ".agents/pi/mcp.json")]
    [InlineData("~", "mcp.json")]
    [InlineData("agent", "project/agent/mcp.json")]
    [InlineData("../agent", "agent/mcp.json")]
    public void should_resolve_the_directory(string configured, string expected)
    {
        var locations = Locations with { Project = HomeFile("project"), Environment = _ => configured };
        var result = DirectMcpClients.Paths("pi", DirectMcpScope.User, locations);
        result.Unsupported.ShouldBeNull();
        result.Paths.Single().ShouldEqual(expected);
    }

    [Theory]
    [InlineData("~/../external-agent")]
    [InlineData("../../external-agent")]
    public void should_refuse_an_outside_home_directory(string configured)
    {
        var locations = Locations with { Project = HomeFile("project"), Environment = _ => configured };
        var result = DirectMcpClients.Paths("pi", DirectMcpScope.User, locations);
        result.Paths.ShouldBeEmpty();
        result.Unsupported.ShouldContain("PI_CODING_AGENT_DIR");
    }
}
