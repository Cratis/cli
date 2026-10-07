// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpCommand;

public class when_rejecting_conflicting_roots : given.a_cli_process
{
    async Task Because() => await Run("screenplay", "mcp", "./models", "--project-root", ".");

    [Fact] void should_keep_standard_output_empty() => _output.ShouldBeEmpty();
    [Fact] void should_explain_the_conflict_on_standard_error() => _error.ShouldContain("Select only one");
    [Fact] void should_exit_unsuccessfully() => _exitCode.ShouldNotEqual(ExitCodes.Success);
}
