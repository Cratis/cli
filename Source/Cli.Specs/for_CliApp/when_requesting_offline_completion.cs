// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliApp;

public class when_requesting_offline_completion : given.a_cli_process
{
    Task Because() => Run("_complete", "output-formats", "--current", "json-compact");

    [Fact] void should_only_write_the_completion_candidate() => _output.ShouldEqual($"json-compact{Environment.NewLine}");
    [Fact] void should_leave_standard_error_empty() => _error.ShouldBeEmpty();
    [Fact] void should_exit_successfully() => _exitCode.ShouldEqual(ExitCodes.Success);
}
