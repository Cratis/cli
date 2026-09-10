// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_vogen_8_is_not_the_canonical_patch : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWithVogenEvidence(
            new ResolvedScreenplayPackage("Marten", "9.29.0"),
            new ResolvedScreenplayPackage("Vogen", "8.1.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.29.2")));

    [Fact] void should_admit_generation() => _result.BlockingDiagnostic.ShouldBeNull();
    [Fact] void should_report_source_reviewed_support() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.SourceReviewed);
    [Fact] void should_keep_recognition_separate() => _result.Provenance.Compatibility!.RecognitionStatus.ShouldEqual(ScreenplayRecognitionStatus.Recognized);
    [Fact] void should_explain_that_the_vogen_set_is_not_exactly_canonical() => _result.Provenance.Compatibility!.Explanation.ShouldEqual("Marten 9.29.0 with WolverineFx 6.29.2 and Vogen 8.1.0 is within source-reviewed major generations but is not an exact canonical package set");
}
