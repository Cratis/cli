// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ValidateScreenplayCommand.when_selecting_a_scope;

public class and_completeness_is_requested : given.a_scoped_cli_process
{
    void Establish() => File.WriteAllText(_document, "module M\n  feature F\n    slice StateView Clean\n      event Changed\n        id Uuid\n    slice StateView Other\n      event OtherChanged\n        id Uuid\n");
    async Task Because() => await Run("--scope", "M.F.Clean", "--check", "event-consumers", "--warnings-as-errors");

    [Fact] void should_fail_for_the_scoped_warning() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_scoped_completeness_finding() => _error.ShouldContain("PLAY0536");
    [Fact] void should_count_only_the_scoped_warning() => Summary.GetProperty("warnings").GetInt32().ShouldEqual(1);
    [Fact] void should_count_completeness_findings_for_the_whole_application() => Summary.GetProperty("wholeApplication").GetProperty("warnings").GetInt32().ShouldEqual(2);
    [Fact] void should_report_that_completeness_ran() => Summary.GetProperty("completenessStatus").GetString().ShouldEqual("ran");
}
