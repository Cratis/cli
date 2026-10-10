// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CliApp;

public class when_requesting_help_with_output_options : for_ScreenplayMcpCommand.given.a_cli_process
{
    [Theory]
    [InlineData("-q")]
    [InlineData("--quiet")]
    [InlineData("-o", "json")]
    [InlineData("--output", "json-compact")]
    [InlineData("--output", "plain")]
    public async Task should_not_add_the_community_pointer(params string[] options)
    {
        await Run(["--help", .. options]);
        _output.ShouldNotContain("discord.gg");
    }
}
