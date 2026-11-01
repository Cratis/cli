// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand.when_starting_the_bridge;

public class with_options : given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke(new() { Url = "https://direct.example", Tenant = "team" });

    [Fact] void should_run_the_bridge_with_the_pinned_options() => _runner.Received(1).Run(new DirectMcpOptions("https://direct.example", "team"), _input, _output, _error, Arg.Any<CancellationToken>());
    [Fact] void should_write_nothing_to_the_protocol_stream() => _output.ToString().ShouldBeEmpty();
    [Fact] void should_leave_standard_error_empty() => _error.ToString().ShouldBeEmpty();
    [Fact] void should_return_success_after_eof() => _exitCode.ShouldEqual(ExitCodes.Success);
}
