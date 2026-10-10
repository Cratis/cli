// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_writing;

public class and_codescene_is_selected : given.captured_install_output
{
    void Because() => Write(OutputFormats.Json, ["cratis/codescene"]);

    [Fact] void should_include_the_registration_note_in_warnings() => Warnings().ShouldContain("CodeScene: the CLI does not register the CodeScene MCP server. Register `cs-mcp` (`brew install cs-mcp`, or `npx -y @codescene/codehealth-mcp`) in your own user-level MCP configuration; do not add it to project files.");
    [Fact] void should_preserve_existing_warnings() => Warnings().ShouldContain("Existing warning");
    [Fact] void should_not_modify_the_result() => _result.Warnings!.ShouldContainOnly("Existing warning");
    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
}
