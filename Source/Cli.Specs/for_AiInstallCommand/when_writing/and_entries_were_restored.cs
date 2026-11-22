// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiInstallCommand.when_writing;

public class and_entries_were_restored : given.captured_install_output
{
    const string Restoration = "Restored rules/general.md";

    void Establish() => _result = new([Restoration], []);

    void Because() => Write(OutputFormats.Plain);

    [Fact] void should_print_the_restoration() => _output.ShouldContain(Restoration);
    [Fact] void should_succeed() => _exitCode.ShouldEqual(ExitCodes.Success);
}
