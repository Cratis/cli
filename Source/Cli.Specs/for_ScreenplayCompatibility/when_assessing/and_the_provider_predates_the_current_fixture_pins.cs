// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_the_provider_predates_the_current_fixture_pins : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    /// <summary>
    /// Cratis.CritterStack.Screenplay v0.27.2 pinned Marten 9.58.0 and WolverineFx 6.63.0, so it does not carry the v0.29.3 fixture pins.
    /// </summary>
    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new a_provider(ScreenplayProviders.CritterStack, "0.27.2"),
        LoadedWith(
            new ResolvedScreenplayPackage("Marten", "9.62.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.64.2"),
            new ResolvedScreenplayPackage("WolverineFx.Marten", "6.64.2")));

    [Fact] void should_admit_generation() => _result.BlockingDiagnostic.ShouldBeNull();
    [Fact] void should_not_claim_canonical_support() => _result.Provenance.Compatibility.SupportTier.ShouldEqual(ScreenplaySupportTier.SourceReviewed);
    [Fact] void should_explain_the_required_provider_baseline() => _result.Provenance.Compatibility.Explanation.ShouldContain("provider 0.28.0 or newer");
}
