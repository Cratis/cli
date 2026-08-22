// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_the_vogen_versions_diverge : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWithVogenEvidence(
            new ResolvedScreenplayPackage("Marten", "9.29.0"),
            new ResolvedScreenplayPackage("Vogen", "8.0.7"),
            new ResolvedScreenplayPackage("Vogen", "8.1.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.29.2")));

    [Fact] void should_fail_closed_as_unknown() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.Unknown);
    [Fact] void should_name_the_divergent_versions() => _result.BlockingDiagnostic!.Message.ShouldContain("Projects resolve divergent Vogen versions: 8.0.7, 8.1.0");
}
