// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_selecting_a_scope;

public class and_the_slice_is_clean_but_the_application_is_not : given.a_scoped_cli_process
{
    async Task Because() => await Run("--scope", "M.F.Clean", "--warnings-as-errors");

    [Fact] void should_pass_for_the_reported_set() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_the_selected_slice() => Summary.GetProperty("scope").GetString().ShouldEqual("M.F.Clean");
    [Fact] void should_report_a_valid_scope() => Summary.GetProperty("valid").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_an_invalid_whole_application() => Summary.GetProperty("wholeApplication").GetProperty("valid").GetBoolean().ShouldBeFalse();
    [Fact] void should_count_whole_application_errors() => Summary.GetProperty("wholeApplication").GetProperty("errors").GetInt32().ShouldEqual(2);
    [Fact] void should_keep_errors_outside_the_scope_out_of_diagnostics() => _error.ShouldNotContain("broken");
}
