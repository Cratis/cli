// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_selecting_a_scope;

public class and_a_module_is_selected : given.a_scoped_cli_process
{
    async Task Because() => await Run("--scope", "M");

    [Fact] void should_fail_for_the_module_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_selected_module() => Summary.GetProperty("scope").GetString().ShouldEqual("M");
    [Fact] void should_include_descendant_errors() => Summary.GetProperty("errors").GetInt32().ShouldEqual(1);
    [Fact] void should_exclude_other_module_diagnostics() => _error.ShouldNotContain("outsideBroken");
    [Fact] void should_count_whole_application_errors() => Summary.GetProperty("wholeApplication").GetProperty("errors").GetInt32().ShouldEqual(2);
}
