// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_CliEntryPoint.when_starting_direct_mcp;

public class with_options : for_CliEntryPoint.given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke("direct", "mcp", "--url", "https://direct.example", "--tenant", "team");

    [Fact] void should_not_initialize_the_cli_that_writes_banners_and_update_hints() => _interactiveStarted.ShouldBeFalse();
    [Fact] void should_run_the_bridge_with_the_pinned_options() => _direct.Received(1).Run(new DirectMcpOptions("https://direct.example", "team"), _input, _output, _error, Arg.Any<CancellationToken>());
    [Fact] void should_write_nothing_to_the_protocol_stream() => _output.ToString().ShouldBeEmpty();
    [Fact] void should_return_success_after_eof() => _exitCode.ShouldEqual(ExitCodes.Success);
}
