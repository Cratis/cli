// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Cli.for_ScreenplayValidation.when_checking_completeness;

public class and_all_is_selected : given.a_folder_with_documents
{
    CompletenessChecks _checks;
    ValidatedScreenplay _result;
    ValidatedScreenplay _executable;

    void Establish()
    {
        CompletenessChecks.TryParse("all", out _checks).ShouldBeTrue();
        WriteDocument("MyApp.play", "module M\n  feature F\n    slice StateView View\n      event Unconsumed\n        id Uuid\n      screen One\n      screen Two\n");
    }

    void Because()
    {
        _result = _validation.Validate(_folder, _checks);
        _executable = _validation.ValidateExecutable(_folder, _checks);
    }

    [Fact] void should_select_every_check() => _result.Checks.Selected.ShouldContainOnly(Enum.GetValues<CompletenessCheck>());
    [Fact] void should_report_both_gaps() => _result.Diagnostics.Select(_ => _.Code).ShouldContainOnly("PLAY0536", "PLAY0537");
    [Fact] void should_report_only_warnings() => _result.Diagnostics.All(_ => _.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_run_completeness_with_executable_binding() => _executable.CompletenessStatus.ShouldEqual("ran");
    [Fact] void should_preserve_findings_with_executable_binding() => _executable.Diagnostics.Select(_ => _.Code).ShouldContain("PLAY0537");
    [Fact] void should_still_bind_the_model() => _executable.Executable.ShouldEqual(true);
}
