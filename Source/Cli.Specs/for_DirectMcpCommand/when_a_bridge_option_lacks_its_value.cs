// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DirectMcpCommand;

public class when_a_bridge_option_lacks_its_value : for_ScreenplayMcpCommand.given.a_cli_process
{
    async Task Because() => await Run("direct", "mcp", "--tenant");

    [Fact] void should_keep_standard_output_empty() => _output.ShouldBeEmpty();
    [Fact] void should_report_the_framework_error_on_standard_error() => _error.ShouldContain("tenant");
    [Fact] void should_exit_unsuccessfully() => _exitCode.ShouldNotEqual(ExitCodes.Success);
}
