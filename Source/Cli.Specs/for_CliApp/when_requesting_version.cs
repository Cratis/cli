// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Version;

namespace Cratis.Cli.for_CliApp;

public class when_requesting_version : given.a_cli_process
{
    Task Because() => Run("--version");

    [Fact] void should_report_the_cli_version() => _output.ShouldContain(VersionCommand.GetCliVersion());
    [Fact] void should_report_the_bundled_screenplay_version() => _output.ShouldContain($"Screenplay {VersionCommand.GetScreenplayVersion()}");
    [Fact] void should_leave_standard_error_empty() => _error.ShouldBeEmpty();
    [Fact] void should_exit_successfully() => _exitCode.ShouldEqual(ExitCodes.Success);
}
