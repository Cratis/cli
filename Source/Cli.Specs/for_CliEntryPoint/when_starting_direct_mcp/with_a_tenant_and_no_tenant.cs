// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_CliEntryPoint.when_starting_direct_mcp;

public class with_a_tenant_and_no_tenant : for_CliEntryPoint.given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke("direct", "mcp", "--no-tenant", "--tenant", "team");

    [Fact] void should_not_run_the_bridge() => _direct.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_print_usage_to_standard_error() => _error.ToString().ShouldContain(DirectMcpInvocation.Usage);
    [Fact] void should_fail_validation() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
