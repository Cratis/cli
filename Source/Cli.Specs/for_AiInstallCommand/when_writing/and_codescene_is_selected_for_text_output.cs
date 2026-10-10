// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_writing;

public class and_codescene_is_selected_for_text_output : given.captured_install_output
{
    void Because() => Write(OutputFormats.Plain, ["cratis/codescene"]);

    [Fact] void should_include_the_registration_note() => _output.ShouldContain("CodeScene: the CLI does not register the CodeScene MCP server.");
    [Fact] void should_include_the_homebrew_install_command() => _output.ShouldContain("brew install cs-mcp");
    [Fact] void should_include_the_npx_command() => _output.ShouldContain("npx -y @codescene/codehealth-mcp");
    [Fact] void should_direct_registration_to_user_configuration() => _output.ShouldContain("in your own user-level MCP configuration; do not add it to project files.");
    [Fact] void should_preserve_existing_warnings() => _output.ShouldContain("Existing warning");
    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
}
