// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpCommand.when_starting_mcp;

public class with_invalid_arguments : given.a_protocol_invocation
{
    void Because() => _exitCode = Invoke(new(), "--unknown");

    [Fact] void should_not_pollute_stdout_with_usage() => _output.ToString().ShouldBeEmpty();
    [Fact] void should_point_at_the_framework_help() => _error.ToString().ShouldContain("cratis screenplay mcp --help");
    [Fact] void should_name_the_unknown_option() => _error.ToString().ShouldContain("--unknown");
    [Fact] void should_not_start_the_server() => _runner.DidNotReceiveWithAnyArgs().Run(default, default!, default!);
    [Fact] void should_return_nonzero() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
