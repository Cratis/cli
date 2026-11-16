// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompatibility.when_assessing;

public class and_the_wolverine_version_ends_with_a_separator : given.compatibility_evidence
{
    ScreenplayCompatibilityEvaluation _result;

    void Because() => _result = ScreenplayCompatibility.Evaluate(
        new CritterStackSourceProvider(),
        LoadedWith(
            new ResolvedScreenplayPackage("Marten", "9.23.0"),
            new ResolvedScreenplayPackage("WolverineFx", "6.29.1+")));

    [Fact] void should_fail_closed_as_unknown() => _result.Provenance.Compatibility.SupportTier.ShouldEqual(ScreenplaySupportTier.Unknown);
    [Fact] void should_report_the_unclassified_version() => _result.BlockingDiagnostic.Message.ShouldEqual("WolverineFx version '6.29.1+' cannot be classified. Generation stopped before source semantics were interpreted");
}
