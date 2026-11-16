// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_package_versions_have_equivalent_case : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWith(
            new ResolvedScreenplayPackage("marten", "9.23.0"),
            new ResolvedScreenplayPackage("MARTEN", "9.23.0"),
            new ResolvedScreenplayPackage("wolverinefx", "6.29.1-PREVIEW"),
            new ResolvedScreenplayPackage("WolverineFx", "6.29.1-preview"),
            new ResolvedScreenplayPackage("wolverinefx.marten", "6.29.1-preview")));

    [Fact] void should_admit_generation() => _result.BlockingDiagnostic.ShouldBeNull();
    [Fact] void should_recognize_the_reviewed_major() => _result.Provenance.Compatibility.SupportTier.ShouldEqual(ScreenplaySupportTier.SourceReviewed);
}
