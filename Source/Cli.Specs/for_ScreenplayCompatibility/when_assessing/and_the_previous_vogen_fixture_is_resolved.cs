// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_the_previous_vogen_fixture_is_resolved : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    /// <summary>
    /// Historical CLI coverage from Cratis.CritterStack.Screenplay 0.26.1 README.md, Compatibility fixtures, VogenConcepts.
    /// </summary>
    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWithVogenEvidence(
            new ResolvedScreenplayPackage("Marten", "9.45.0"),
            new ResolvedScreenplayPackage("Vogen", "8.0.7"),
            new ResolvedScreenplayPackage("WolverineFx", "6.45.0")));

    [Fact] void should_admit_generation() => _result.BlockingDiagnostic.ShouldBeNull();
    [Fact] void should_keep_reporting_canonical_support() => _result.Provenance.Compatibility.SupportTier.ShouldEqual(ScreenplaySupportTier.Canonical);
    [Fact] void should_keep_recognition_separate() => _result.Provenance.Compatibility.RecognitionStatus.ShouldEqual(ScreenplayRecognitionStatus.Recognized);
}
