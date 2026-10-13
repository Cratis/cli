// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand;

public class when_rejecting_unknown_options : given.a_completeness_cli_process
{
    async Task Because() => await Run("--unknown");

    [Fact] void should_exit_with_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_name_the_unknown_option() => _error.ShouldContain("--unknown");
    [Fact] void should_point_at_command_help() => _error.ShouldContain("cratis screenplay validate --help");
    [Fact] void should_not_compile_the_model() => _output.ShouldBeEmpty();
}
