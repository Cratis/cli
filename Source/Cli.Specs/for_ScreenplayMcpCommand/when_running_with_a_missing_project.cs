// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpCommand;

public class when_running_with_a_missing_project : given.a_cli_process
{
    string _missing;

    void Establish() => _missing = Path.Combine(Path.GetTempPath(), $"missing-mcp-{Guid.NewGuid():N}");
    async Task Because() => await Run("screenplay", "mcp", "--project-root", _missing);

    [Fact] void should_not_pollute_standard_output() => _output.ShouldBeEmpty();
    [Fact] void should_name_the_missing_project_on_standard_error() => _error.ShouldContain(_missing);
    [Fact] void should_exit_with_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
