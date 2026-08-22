// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_the_marten_version_is_malformed : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new MartenSourceProvider(),
        LoadedWith(new ResolvedScreenplayPackage("Marten", "9.not-a-version")));

    [Fact] void should_fail_closed_as_unknown() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.Unknown);
    [Fact] void should_report_unknown_version_evidence() => _result.BlockingDiagnostic!.Code.ShouldEqual(ScreenplayDiagnosticCodes.UnknownFrameworkVersion);
}
