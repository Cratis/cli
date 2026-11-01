// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectMcpCommand;

public class when_an_unknown_bridge_option_is_given : for_ScreenplayMcpCommand.given.a_cli_process
{
    async Task Because() => await Run("direct", "mcp", "--verbose", "yes");

    [Fact] void should_keep_standard_output_empty() => _output.ShouldBeEmpty();
    [Fact] void should_name_the_unknown_option() => _error.ShouldContain("--verbose");
    [Fact] void should_print_usage_to_standard_error() => _error.ShouldContain(DirectMcpInvocation.Usage);
    [Fact] void should_exit_with_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
