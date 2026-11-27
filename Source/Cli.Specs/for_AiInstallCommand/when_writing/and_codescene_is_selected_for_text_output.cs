// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_writing;

public class and_codescene_is_selected_for_text_output : given.captured_install_output
{
    void Because() => Write(OutputFormats.Plain, ["cratis/codescene"]);

    [Fact] void should_include_the_registration_note() => _output.ShouldContain("CodeScene: the project MCP configuration starts the");
    [Fact] void should_include_the_homebrew_install_command() => _output.ShouldContain("brew install cs-mcp");
    [Fact] void should_ask_for_sign_in() => _output.ShouldContain("sign in with your CodeScene account");
    [Fact] void should_preserve_existing_warnings() => _output.ShouldContain("Existing warning");
    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
}
