// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_writing;

public class and_codescene_is_selected : given.captured_install_output
{
    void Because() => Write(OutputFormats.Json, ["cratis/codescene"]);

    [Fact] void should_include_the_registration_note_in_warnings() => Warnings().ShouldContain("CodeScene: the project MCP configuration starts the `cs-mcp` command. Install it (for example `brew install cs-mcp`) and sign in with your CodeScene account before using the server.");
    [Fact] void should_preserve_existing_warnings() => Warnings().ShouldContain("Existing warning");
    [Fact] void should_not_modify_the_result() => _result.Warnings.ShouldContainOnly("Existing warning");
    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
}
