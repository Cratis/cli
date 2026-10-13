// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Cli.for_ScreenplayValidation.when_checking_completeness;

public class and_navigation_is_selected : given.a_folder_with_documents
{
    ValidatedScreenplay _selected;
    ValidatedScreenplay _unselected;
    ValidatedScreenplay _other;

    void Establish() => WriteDocument("MyApp.play", "module M\n  feature F\n    slice StateView View\n      screen One\n      screen Two\n");

    void Because()
    {
        _selected = _validation.Validate(_folder, new([CompletenessCheck.Navigation]));
        _unselected = _validation.Validate(_folder);
        _other = _validation.Validate(_folder, new([CompletenessCheck.EventConsumers]));
    }

    [Fact] void should_report_the_navigation_warning() => _selected.Diagnostics.Single().Code.ShouldEqual("PLAY0537");
    [Fact] void should_keep_the_finding_a_warning() => _selected.Diagnostics.Single().Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_preserve_the_compiler_message() => _selected.Diagnostics.Single().Message.ShouldContain("none of its 2 screens is reachable");
    [Fact] void should_report_that_checks_ran() => _selected.CompletenessStatus.ShouldEqual("ran");
    [Fact] void should_not_report_opt_in_findings_by_default() => _unselected.Diagnostics.ShouldBeEmpty();
    [Fact] void should_report_that_checks_were_not_requested() => _unselected.CompletenessStatus.ShouldEqual("not requested");
    [Fact] void should_not_report_unselected_findings() => _other.Diagnostics.ShouldBeEmpty();
}
