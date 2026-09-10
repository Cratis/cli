// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_resolved_package_provenance_is_missing : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new MartenSourceProvider(),
        LoadedWith());

    [Fact] void should_fail_closed() => _result.BlockingDiagnostic.ShouldNotBeNull();
    [Fact] void should_report_unknown_provenance() => _result.BlockingDiagnostic!.Code.ShouldEqual(ScreenplayDiagnosticCodes.UnknownFrameworkVersion);
    [Fact] void should_keep_unknown_distinct_from_unsupported() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.Unknown);
    [Fact] void should_not_evaluate_semantic_conformance() => _result.Provenance.Compatibility!.SemanticConformance.ShouldEqual(ScreenplaySemanticConformance.NotEvaluated);
}
