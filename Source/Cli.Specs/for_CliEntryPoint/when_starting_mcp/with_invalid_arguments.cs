// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliEntryPoint.when_starting_mcp;

public class with_invalid_arguments : given.a_protocol_invocation
{
    async Task Because() => _exitCode = await Invoke("screenplay", "mcp", "--unknown");

    [Fact] void should_not_pollute_stdout_with_usage() => _output.ToString().ShouldBeEmpty();
    [Fact] void should_write_usage_to_stderr() => _error.ToString().ShouldContain("Usage:");
    [Fact] void should_not_initialize_interactive_cli_or_update_check() => _interactiveStarted.ShouldBeFalse();
    [Fact] void should_not_start_the_server() => _runner.DidNotReceiveWithAnyArgs().Run(default!, default!, default!);
    [Fact] void should_return_nonzero() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
