// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_generating;

public class from_marten_source : given.a_marten_application_built_from_source
{
    GeneratedScreenplay _result = null!;

    void Because() => _result = CritterStackScreenplayGeneration.GenerateFrom(
        Loaded,
        "/workspace/Banking/Banking.csproj",
        ScreenplayGenerationOptions.Default with { Provider = ScreenplayProviders.Marten });

    [Fact] void should_report_the_project() => _result.Projects.ShouldContainOnly(ProjectName);
    [Fact] void should_generate_the_read_model() => _result.Source.ShouldContain("readmodel Account");
    [Fact] void should_generate_the_event() => _result.Source.ShouldContain("event AccountOpened");
    [Fact] void should_generate_the_reducer() => _result.Source.ShouldContain("reducer AccountSnapshot => Account");
    [Fact] void should_report_the_unlowered_aggregate_role() => _result.Diagnostics.ShouldContainOnly(
        new ScreenplayDiagnostic(
            ScreenplayDiagnosticSeverity.Warning,
            "GEN0004",
            "The recognized Aggregate artifact 'Account' cannot yet be represented by the Screenplay lowerer and was omitted",
            "Account.cs"));
}
