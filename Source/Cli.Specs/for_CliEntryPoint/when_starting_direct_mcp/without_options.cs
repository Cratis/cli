// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_CliEntryPoint.when_starting_direct_mcp;

public class without_options : for_CliEntryPoint.given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke("direct", "mcp");

    [Fact] void should_not_initialize_the_cli() => _interactiveStarted.ShouldBeFalse();
    [Fact] void should_run_the_bridge_for_the_active_login() => _direct.Received(1).Run(new DirectMcpOptions(null, null), _input, _output, _error, Arg.Any<CancellationToken>());
}
