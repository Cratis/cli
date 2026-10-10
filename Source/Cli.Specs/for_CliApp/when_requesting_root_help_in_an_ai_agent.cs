// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliApp;

public class when_requesting_root_help_in_an_ai_agent : given.a_cli_process
{
    void Establish() => _environment["CLAUDECODE"] = "1";

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public async Task should_render_framework_help_without_the_community_pointer(string option)
    {
        await Run(option);
        _output.ShouldContain("USAGE:");
        _output.ShouldNotContain("discord.gg");
        _error.ShouldBeEmpty();
        _exitCode.ShouldEqual(ExitCodes.Success);
    }
}
