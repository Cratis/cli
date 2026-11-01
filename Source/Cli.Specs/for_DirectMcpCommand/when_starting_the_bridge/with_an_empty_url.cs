// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand.when_starting_the_bridge;

public class with_an_empty_url : given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke(new() { Url = " " });

    [Fact] void should_not_run_the_bridge() => _runner.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_print_usage_to_standard_error() => _error.ToString().ShouldContain(DirectMcpInvocation.Usage);
    [Fact] void should_fail_validation() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
