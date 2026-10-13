// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_selecting_a_scope;

[Collection(CliSpecsCollection.Name)]
public class and_console_colors_are_enabled : given.a_scoped_console
{
    async Task Because() => await CaptureScopedConsole();

    [Fact] void should_keep_the_scoped_exit_code() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_color_the_invalid_whole_application_summary() => _output.ShouldContain("\u001b[38;2;255;85;85mWhole application: 2 error(s), 0 warning(s) (2 outside the reported set)");
    [Fact] void should_report_consumer_scopes_including_application_declarations() => _output.ShouldContain("2 reference(s) in <application>, Other.F");
    [Fact] void should_report_possibly_affected_references() => _output.ShouldContain("Possibly affected: 3 other unresolved reference(s) outside the reported declarations");
}
