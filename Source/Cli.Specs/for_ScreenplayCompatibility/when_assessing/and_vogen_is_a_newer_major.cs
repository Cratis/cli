// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_vogen_is_a_newer_major : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWithVogenEvidence(
            new ResolvedScreenplayPackage("Marten", "9.29.0"),
            new ResolvedScreenplayPackage("Vogen", "9.0.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.29.2")));

    [Fact] void should_fail_closed_as_unsupported() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.Unsupported);
    [Fact] void should_keep_recognition_separate() => _result.Provenance.Compatibility!.RecognitionStatus.ShouldEqual(ScreenplayRecognitionStatus.Unsupported);
    [Fact] void should_explain_the_reviewed_major_boundary() => _result.BlockingDiagnostic!.Message.ShouldContain("Vogen 9.0.0 is newer than the highest source-reviewed major (8)");
    [Fact] void should_not_evaluate_lowering() => _result.Provenance.Compatibility!.LoweringFidelity.ShouldEqual(ScreenplayLoweringFidelity.NotEvaluated);
}
