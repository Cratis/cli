// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpCommand;

public class when_rejecting_unknown_options : given.a_cli_process
{
    async Task Because() => await Run("screenplay", "mcp", "--unknown");

    [Fact] void should_keep_standard_output_empty() => _output.ShouldBeEmpty();
    [Fact] void should_name_the_unknown_option() => _error.ShouldContain("--unknown");
    [Fact] void should_point_at_framework_help() => _error.ShouldContain("cratis screenplay mcp --help");
    [Fact] void should_exit_with_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
