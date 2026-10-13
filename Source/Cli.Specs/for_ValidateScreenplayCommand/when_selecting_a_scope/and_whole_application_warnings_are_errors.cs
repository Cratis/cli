// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_selecting_a_scope;

[Collection(CliSpecsCollection.Name)]
public class and_whole_application_warnings_are_errors : given.a_scoped_console
{
    void Establish()
    {
        _errors = 0;
        _warnings = 1;
        _settings.WarningsAsErrors = true;
    }

    async Task Because() => await CaptureScopedConsole();

    [Fact] void should_keep_the_scoped_exit_code() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_color_the_invalid_whole_application_summary() => _output.ShouldContain("\u001b[38;2;255;85;85mWhole application: 0 error(s), 1 warning(s) (1 outside the reported set)");
}
