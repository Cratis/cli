// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpRegistration.when_uninstalling;

public class with_an_ownership_record_outside_the_home_directory : given.a_home_and_a_project
{
    Exception _error;

    void Establish() =>
        Write(HomeFile(DirectMcpManifest.RelativePath), "{\"Servers\":[{\"Harness\":\"codex\",\"Path\":\"tools/../../outside/config.toml\",\"Collection\":\"mcp_servers\",\"Id\":\"cratis-direct\",\"Installed\":{\"command\":\"cratis\",\"args\":[\"direct\",\"mcp\"]}}]}");

    void Because() => _error = Catch.Exception(() => Uninstall(DirectMcpScope.User));

    [Fact] void should_refuse_the_record() => _error.Message.ShouldContain("Invalid Direct MCP ownership record");
}
