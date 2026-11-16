// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_marten_versions_diverge : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new MartenSourceProvider(),
        LoadedWith(
            new ResolvedScreenplayPackage("Marten", "9.23.0"),
            new ResolvedScreenplayPackage("Marten", "9.20.1")));

    [Fact] void should_fail_closed_as_unknown() => _result.Provenance.Compatibility.SupportTier.ShouldEqual(ScreenplaySupportTier.Unknown);
    [Fact] void should_sort_the_reported_versions() => _result.BlockingDiagnostic.Message.ShouldEqual("Projects resolve divergent Marten versions: 9.20.1, 9.23.0. Generation stopped before source semantics were interpreted");
}
