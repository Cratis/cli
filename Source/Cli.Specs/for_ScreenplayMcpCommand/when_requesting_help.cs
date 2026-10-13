// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayMcpCommand;

public class when_requesting_help : given.a_cli_process
{
    async Task Because() => await Run("screenplay", "mcp", "--help");

    [Fact] void should_render_framework_help_on_standard_output() => _output.ShouldContain("USAGE:");
    [Fact] void should_describe_the_project_root_option() => _output.ShouldContain("--project-root");
    [Fact] void should_not_advertise_global_output_options() => _output.ShouldNotContain("--output");
    [Fact] void should_not_include_the_root_community_pointer() => _output.ShouldNotContain("discord.gg");
    [Fact] void should_leave_standard_error_empty() => _error.ShouldBeEmpty();
    [Fact] void should_exit_successfully() => _exitCode.ShouldEqual(ExitCodes.Success);
}
