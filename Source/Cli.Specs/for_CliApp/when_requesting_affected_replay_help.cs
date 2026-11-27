// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliApp;

public class when_requesting_affected_replay_help : given.a_cli_process
{
    async Task Because() => await Run("affected", "replay", "--help");

    [Fact] void should_exit_successfully() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_not_report_markup_errors() => _output.ShouldNotContain("Could not find color or style");
}
