// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_the_package_set_is_source_reviewed : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWith(
            new ResolvedScreenplayPackage("Marten", "9.29.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.29.2")));

    [Fact] void should_admit_generation() => _result.BlockingDiagnostic.ShouldBeNull();
    [Fact] void should_not_promote_it_to_canonical() => _result.Provenance.Compatibility!.SupportTier.ShouldEqual(ScreenplaySupportTier.SourceReviewed);
    [Fact] void should_explain_that_the_exact_set_is_not_canonical() => _result.Provenance.Compatibility!.Explanation.ShouldContain("not an exact canonical package set");
}
