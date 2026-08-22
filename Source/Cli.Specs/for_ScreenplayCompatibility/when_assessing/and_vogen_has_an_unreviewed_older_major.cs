// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_vogen_has_an_unreviewed_older_major : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWithVogenEvidence(
            new ResolvedScreenplayPackage("Marten", "9.29.0"),
            new ResolvedScreenplayPackage("Vogen", "7.5.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.29.2")));

    [Fact] void should_fail_closed_as_unknown() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.Unknown);
    [Fact] void should_not_call_the_unreviewed_major_unsupported() => _result.Provenance.Compatibility!.RecognitionStatus.ShouldEqual(ScreenplayRecognitionStatus.Unknown);
    [Fact] void should_explain_the_missing_review_evidence() => _result.BlockingDiagnostic!.Message.ShouldContain("Vogen 7.5.0 has no canonical or source-reviewed major-generation evidence");
}
