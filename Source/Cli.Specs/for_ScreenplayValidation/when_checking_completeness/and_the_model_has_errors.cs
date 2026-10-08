// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Cli.for_ScreenplayValidation.when_checking_completeness;

public class and_the_model_has_errors : given.a_folder_with_documents
{
    ValidatedScreenplay _result;

    void Establish() => WriteDocument("MyApp.play", InvalidSource);
    void Because() => _result = _validation.Validate(_folder, CompletenessChecks.All);

    [Fact] void should_keep_source_errors() => _result.Diagnostics.Any(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_skip_completeness() => _result.CompletenessStatus.ShouldEqual("skipped");
    [Fact] void should_report_the_source_error_count() => _result.CompletenessNote.ShouldEqual($"completeness checks skipped: the model has {_result.Diagnostics.Count(_ => _.Severity == ScreenplayDiagnosticSeverity.Error)} error(s)");
    [Fact] void should_not_report_completeness_findings() => _result.Diagnostics.Any(_ => Enumerable.Range(530, 8).Select(code => $"PLAY{code:D4}").Contains(_.Code, StringComparer.Ordinal)).ShouldBeFalse();
}
