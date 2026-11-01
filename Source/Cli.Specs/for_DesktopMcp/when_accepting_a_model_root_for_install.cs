// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_accepting_a_model_root_for_install : given.a_cli_process
{
    async Task Because() => await Run("screenplay", "desktop", "install", "--model-root", "models", "--clients", "unknown", "--version", "4.55.0", "--dry-run");

    [Fact] void should_accept_the_option_and_reach_client_selection() => _error.ShouldContain("Select --clients claude,chatgpt");
    [Fact] void should_not_report_an_unknown_option() => _error.ShouldNotContain("Unknown option");
    [Fact] void should_stop_before_any_desktop_mutation() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_write_client_results() => _output.ShouldBeEmpty();
}
