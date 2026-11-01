// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_selecting_a_scope;

public class and_errors_prevent_completeness : given.a_scoped_cli_process
{
    async Task Because() => await Run("--scope", "M.F.Clean", "--check", "all");

    [Fact] void should_pass_for_the_clean_scope() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_skipped_completeness() => Summary.GetProperty("completenessStatus").GetString().ShouldEqual("skipped");
    [Fact] void should_explain_the_whole_application_error_count() => Summary.GetProperty("completenessNote").GetString().ShouldEqual("completeness checks skipped: the model has 2 error(s)");
    [Fact] void should_not_report_completeness_findings() => _error.ShouldNotContain("PLAY053");
}
