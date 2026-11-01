// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_writing;

public class and_quiet_output_is_requested : given.captured_install_output
{
    void Because() => Write(OutputFormats.Quiet, ["cratis/codescene"]);

    [Fact] void should_not_print_the_note() => _output.ShouldBeEmpty();
    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
}
