// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand.when_starting_the_bridge;

public class with_no_tenant : given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke(new() { Url = "https://direct.example", NoTenant = true });

    [Fact] void should_run_the_bridge_pinned_to_no_tenant() => _runner.Received(1).Run(new DirectMcpOptions("https://direct.example", null, NoTenant: true), _input, _output, _error, Arg.Any<CancellationToken>());
    [Fact] void should_return_success_after_eof() => _exitCode.ShouldEqual(ExitCodes.Success);
}
