// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_selecting_a_scope;

public class and_console_output_is_requested : given.a_scoped_cli_process
{
    void Establish() => _format = OutputFormats.Table;
    async Task Because() => await Run("--scope", "M.F.Clean");

    [Fact] void should_pass_for_the_scope() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_label_scope_counts() => _output.ShouldContain("In scope M.F.Clean: 0 error(s), 0 warning(s)");
    [Fact] void should_show_whole_application_counts() => _output.ShouldContain("Whole application: 2 error(s), 0 warning(s) (2 outside the reported set)");
    [Fact] void should_report_possibly_affected_references() => _output.ShouldContain("Possibly affected: 0 other unresolved reference(s)");
    [Fact] void should_report_unresolved_event_consumers() => _output.ShouldContain("Unresolved event consumers");
}
