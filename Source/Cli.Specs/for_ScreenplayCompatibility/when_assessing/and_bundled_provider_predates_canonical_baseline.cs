// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_bundled_provider_predates_canonical_baseline : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new a_provider(ScreenplayProviders.CritterStack, "0.1.0"),
        LoadedWith(
            new ResolvedScreenplayPackage("Marten", "9.23.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.29.1"),
            new ResolvedScreenplayPackage("WolverineFx.Marten", "6.29.1")));

    [Fact] void should_not_claim_canonical_support() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.SourceReviewed);
    [Fact] void should_explain_the_required_provider_baseline() => _result.Provenance.Compatibility!.Explanation.ShouldContain("provider 0.3.0 or newer");
}
