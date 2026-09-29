// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliEntryPoint.when_starting_direct_mcp;

public class with_help : for_CliEntryPoint.given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke("direct", "mcp", "--tenant", "team", "--help");

    [Fact] void should_leave_help_to_the_interactive_cli() => _interactiveStarted.ShouldBeTrue();
    [Fact] void should_not_run_the_bridge() => _direct.ReceivedCalls().ShouldBeEmpty();
}
