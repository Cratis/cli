// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand.when_starting_the_bridge;

public class with_an_unknown_option : given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke(new(), "--verbose", "yes");

    [Fact] void should_not_run_the_bridge() => _runner.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_write_nothing_to_the_protocol_stream() => _output.ToString().ShouldBeEmpty();
    [Fact] void should_name_the_unknown_option() => _error.ToString().ShouldContain("Unknown option '--verbose'");
    [Fact] void should_point_at_framework_help() => _error.ToString().ShouldContain("cratis direct mcp --help");
    [Fact] void should_print_usage_to_standard_error() => _error.ToString().ShouldContain(DirectMcpInvocation.Usage);
    [Fact] void should_fail_validation() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
