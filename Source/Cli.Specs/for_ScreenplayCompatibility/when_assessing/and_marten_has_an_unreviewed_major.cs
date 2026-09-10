// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_marten_has_an_unreviewed_major : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new MartenSourceProvider(),
        LoadedWith(new ResolvedScreenplayPackage("Marten", "8.12.0")));

    [Fact] void should_fail_closed() => _result.BlockingDiagnostic.ShouldNotBeNull();
    [Fact] void should_report_unknown_version_evidence() => _result.BlockingDiagnostic!.Code.ShouldEqual(ScreenplayDiagnosticCodes.UnknownFrameworkVersion);
    [Fact] void should_not_call_the_unreviewed_major_unsupported() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.Unknown);
}
