// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_completing;

public class and_vogen_lowering_reports_loss : given.compatibility_evidence
{
    ScreenplayGenerationProvenance _result;

    void Because()
    {
        var evaluation = ScreenplayCompatibility.Evaluate(
            new a_provider(ScreenplayProviders.CritterStack, "0.17.0"),
            LoadedWithVogenEvidence(
                new ResolvedScreenplayPackage("Marten", "9.29.0"),
                new ResolvedScreenplayPackage("Vogen", "8.0.7"),
                new ResolvedScreenplayPackage("WolverineFx", "6.29.2")));
        _result = evaluation.Complete(
            [new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Warning, "VOG0002", "normalization omitted", null)]);
    }

    [Fact] void should_retain_the_canonical_support_tier() => _result.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.Canonical);
    [Fact] void should_report_lowering_loss_separately() => _result.Compatibility!.LoweringFidelity.ShouldEqual(ScreenplayLoweringFidelity.LossReported);
    [Fact] void should_still_require_semantic_review() => _result.Compatibility!.SemanticConformance.ShouldEqual(ScreenplaySemanticConformance.RequiresHumanReview);
}
