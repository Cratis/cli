// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_the_current_community_kitchen_package_set_is_resolved : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    /// <summary>
    /// Cratis.CritterStack.Screenplay v0.29.3: README.md, Compatibility fixtures, CommunityKitchen.
    /// </summary>
    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWith(
            new ResolvedScreenplayPackage("Marten", "9.62.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.64.2"),
            new ResolvedScreenplayPackage("WolverineFx.Marten", "6.64.2")));

    [Fact] void should_admit_generation() => _result.BlockingDiagnostic.ShouldBeNull();
    [Fact] void should_report_canonical_support() => _result.Provenance.Compatibility.SupportTier.ShouldEqual(ScreenplaySupportTier.Canonical);
    [Fact] void should_keep_recognition_separate() => _result.Provenance.Compatibility.RecognitionStatus.ShouldEqual(ScreenplayRecognitionStatus.Recognized);
    [Fact] void should_still_require_semantic_review() => _result.Provenance.Compatibility.SemanticConformance.ShouldEqual(ScreenplaySemanticConformance.RequiresHumanReview);
}
