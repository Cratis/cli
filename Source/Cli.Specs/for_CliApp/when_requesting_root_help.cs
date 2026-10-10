// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliApp;

public class when_requesting_root_help : given.a_cli_process
{
    async Task Because() => await Run("--help");

    [Fact] void should_render_framework_help() => _output.ShouldContain("USAGE:");
    [Fact] void should_end_with_the_community_pointer() => _output.TrimEnd().EndsWith("Questions? Ask on Discord: https://discord.gg/kt4AMpV8WV", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_honor_no_color() => _output.Contains("\u001b[", StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_leave_standard_error_empty() => _error.ShouldBeEmpty();
    [Fact] void should_exit_successfully() => _exitCode.ShouldEqual(ExitCodes.Success);
}
