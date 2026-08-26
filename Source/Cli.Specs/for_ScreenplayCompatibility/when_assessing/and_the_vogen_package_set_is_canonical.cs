// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_the_vogen_package_set_is_canonical : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWithVogenEvidence(
            new ResolvedScreenplayPackage("Marten", "9.29.0"),
            new ResolvedScreenplayPackage("Vogen", "8.0.7"),
            new ResolvedScreenplayPackage("WolverineFx", "6.29.2")));

    [Fact] void should_admit_generation() => _result.BlockingDiagnostic.ShouldBeNull();
    [Fact] void should_report_canonical_support() => _result.Provenance.Compatibility.SupportTier.ShouldEqual(ScreenplaySupportTier.Canonical);
    [Fact] void should_keep_package_recognition_separate() => _result.Provenance.Compatibility.RecognitionStatus.ShouldEqual(ScreenplayRecognitionStatus.Recognized);
    [Fact] void should_keep_semantic_review_separate() => _result.Provenance.Compatibility.SemanticConformance.ShouldEqual(ScreenplaySemanticConformance.RequiresHumanReview);
    [Fact] void should_not_claim_lowering_before_generation() => _result.Provenance.Compatibility.LoweringFidelity.ShouldEqual(ScreenplayLoweringFidelity.NotEvaluated);
    [Fact] void should_explain_the_exact_vogen_baseline() => _result.Provenance.Compatibility.Explanation.ShouldContain("Vogen 8.0.7 matches a pinned canonical package set for bundled provider 0.23.0");
}
