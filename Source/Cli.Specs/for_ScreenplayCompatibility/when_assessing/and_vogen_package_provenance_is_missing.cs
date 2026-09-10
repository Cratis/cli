// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_vogen_package_provenance_is_missing : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWithVogenEvidence(
            new ResolvedScreenplayPackage("Cratis.Screenplay.Generation.DotNet.Vogen", "0.7.0"),
            new ResolvedScreenplayPackage("Marten", "9.29.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.29.2")));

    [Fact] void should_fail_closed() => _result.BlockingDiagnostic.ShouldNotBeNull();
    [Fact] void should_report_unknown_support() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.Unknown);
    [Fact] void should_not_treat_the_adapter_as_application_vogen_evidence() => _result.BlockingDiagnostic!.Message.ShouldContain("resolved NuGet package version was not found");
    [Fact] void should_not_evaluate_semantics() => _result.Provenance.Compatibility!.SemanticConformance.ShouldEqual(ScreenplaySemanticConformance.NotEvaluated);
    [Fact] void should_not_evaluate_lowering() => _result.Provenance.Compatibility!.LoweringFidelity.ShouldEqual(ScreenplayLoweringFidelity.NotEvaluated);
}
