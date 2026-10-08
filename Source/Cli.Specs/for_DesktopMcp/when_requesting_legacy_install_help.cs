// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_requesting_legacy_install_help : given.a_cli_process
{
    async Task Because() => await Run("screenplay", "mcp", "install", "--help");

    [Fact] void should_show_framework_help_on_standard_output() => _output.ShouldContain("USAGE:");
    [Fact] void should_show_the_new_route() => _output.ShouldContain("cratis screenplay desktop install");
    [Fact] void should_describe_the_model_root() => _output.ShouldContain("--model-root");
    [Fact] void should_write_the_deprecation_notice_on_standard_error() => _error.ShouldContain("is deprecated");
    [Fact] void should_exit_successfully() => _exitCode.ShouldEqual(ExitCodes.Success);
}
