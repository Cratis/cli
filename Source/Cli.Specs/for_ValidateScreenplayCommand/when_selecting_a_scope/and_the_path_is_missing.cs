// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_selecting_a_scope;

public class and_the_path_is_missing : given.a_scoped_cli_process
{
    void Establish() => File.Delete(_document);
    async Task Because() => await Run("--scope", "M");

    [Fact] void should_return_the_cli_usage_error() => _exitCode.ShouldEqual(ExitCodes.NotFound);
    [Fact] void should_not_report_a_successful_summary() => _output.ShouldBeEmpty();
}
