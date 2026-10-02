// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_CliEntryPoint.when_starting_direct_mcp;

public class with_an_unknown_option : for_CliEntryPoint.given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke("direct", "mcp", "--verbose", "yes");

    [Fact] void should_not_run_the_bridge() => _direct.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_write_nothing_to_the_protocol_stream() => _output.ToString().ShouldBeEmpty();
    [Fact] void should_print_usage_to_standard_error() => _error.ToString().ShouldContain(DirectMcpInvocation.Usage);
    [Fact] void should_fail_validation() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
