// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand.when_starting_the_bridge;

public class without_options : given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke(new());

    [Fact] void should_run_the_bridge_for_the_active_login() => _runner.Received(1).Run(new DirectMcpOptions(null, null), _input, _output, _error, Arg.Any<CancellationToken>());
    [Fact] void should_return_success_after_eof() => _exitCode.ShouldEqual(ExitCodes.Success);
}
