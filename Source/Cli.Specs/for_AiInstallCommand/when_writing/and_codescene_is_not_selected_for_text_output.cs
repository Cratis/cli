// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_writing;

public class and_codescene_is_not_selected_for_text_output : given.captured_install_output
{
    void Because() => Write(OutputFormats.Plain, ["cratis", "cratis/full"]);

    [Fact] void should_not_include_the_registration_note() => _output.ShouldNotContain("CodeScene:");
    [Fact] void should_preserve_existing_warnings() => _output.ShouldContain("Existing warning");
    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
}
