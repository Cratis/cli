// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_marten_is_a_newer_major : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new MartenSourceProvider(),
        LoadedWith(new ResolvedScreenplayPackage("Marten", "10.0.0")));

    [Fact] void should_fail_closed() => _result.BlockingDiagnostic.ShouldNotBeNull();
    [Fact] void should_report_the_unsupported_version_code() => _result.BlockingDiagnostic!.Code.ShouldEqual(ScreenplayDiagnosticCodes.UnsupportedFrameworkVersion);
    [Fact] void should_report_unsupported_support() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.Unsupported);
    [Fact] void should_not_evaluate_lowering() => _result.Provenance.Compatibility!.LoweringFidelity.ShouldEqual(ScreenplayLoweringFidelity.NotEvaluated);
}
