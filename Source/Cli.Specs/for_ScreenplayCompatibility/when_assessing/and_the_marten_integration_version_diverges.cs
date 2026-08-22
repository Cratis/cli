// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_the_marten_integration_version_diverges : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWith(
            new ResolvedScreenplayPackage("Marten", "9.23.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.29.1"),
            new ResolvedScreenplayPackage("WolverineFx.Marten", "6.28.0")));

    [Fact] void should_fail_closed_as_unknown() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.Unknown);
    [Fact] void should_name_both_versions() => _result.BlockingDiagnostic!.Message.ShouldContain("6.28.0 does not match WolverineFx 6.29.1");
}
