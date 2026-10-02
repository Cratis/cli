// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliEntryPoint.when_starting_direct_mcp;

public class with_a_subcommand : for_CliEntryPoint.given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke("direct", "mcp", "install", "--client", "claude");

    [Fact] void should_start_the_interactive_cli() => _interactiveStarted.ShouldBeTrue();
    [Fact] void should_not_run_the_bridge() => _direct.ReceivedCalls().ShouldBeEmpty();
}
