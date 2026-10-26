// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliApp;

public class when_requesting_render_help : given.a_cli_process
{
    async Task Because() => await Run("render", "--help");

    [Fact] void should_exit_successfully() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_render_options() => _output.ShouldContain("OPTIONS:");
    [Fact] void should_advertise_strict_acceptance() => _output.ShouldContain("--strict");
    [Fact] void should_explain_the_exit_precedence() => string.Join(' ', _output.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ShouldContain("errors 1/5/10 > strict_unaccepted 9 > changes_pending 6 > success 0");
    [Fact] void should_explain_that_strict_acceptance_requires_bundled_support() => _output.ShouldContain("Stage acceptance support");
    [Fact] void should_not_report_markup_errors() => _output.ShouldNotContain("Error:");
    [Fact] void should_leave_standard_error_empty() => _error.ShouldBeEmpty();
}
