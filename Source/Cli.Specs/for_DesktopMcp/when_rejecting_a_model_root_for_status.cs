// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_rejecting_a_model_root_for_status : given.a_cli_process
{
    async Task Because() => await Run("screenplay", "desktop", "status", "--model-root", "models", "--version", "4.55.0");

    [Fact] void should_reject_the_option_before_inspecting_clients() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_name_the_unrecognized_option() => _error.ShouldContain("--model-root");
    [Fact] void should_point_at_framework_help() => _error.ShouldContain("cratis screenplay desktop status --help");
    [Fact] void should_not_inspect_clients() => _output.ShouldBeEmpty();
}
