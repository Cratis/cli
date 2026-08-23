// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_the_critter_stack_package_set_is_canonical : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWith(
            new ResolvedScreenplayPackage("Marten", "9.23.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.29.1"),
            new ResolvedScreenplayPackage("WolverineFx.Marten", "6.29.1")));

    [Fact] void should_admit_generation() => _result.BlockingDiagnostic.ShouldBeNull();
    [Fact] void should_report_the_bundled_provider_version() => _result.Provenance.ProviderVersion.ShouldEqual("0.19.0");
    [Fact] void should_report_canonical_support() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.Canonical);
    [Fact] void should_keep_recognition_separate() => _result.Provenance.Compatibility!.RecognitionStatus.ShouldEqual(ScreenplayRecognitionStatus.Recognized);
    [Fact] void should_require_semantic_review() => _result.Provenance.Compatibility!.SemanticConformance.ShouldEqual(ScreenplaySemanticConformance.RequiresHumanReview);
    [Fact] void should_not_claim_lowering_before_generation() => _result.Provenance.Compatibility!.LoweringFidelity.ShouldEqual(ScreenplayLoweringFidelity.NotEvaluated);
}
