// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.when_generating;

public class from_authored_vogen_metadata : given.a_vogen_critter_stack_application_built_from_source
{
    GeneratedScreenplay _result = null!;

    void Because() => _result = CritterStackScreenplayGeneration.GenerateFrom(
        Loaded,
        "/workspace/Ordering/Ordering.csproj",
        ScreenplayGenerationOptions.Default with { Provider = ScreenplayProviders.CritterStack });

    [Fact] void should_generate_the_generic_vogen_concept() => _result.Source.ShouldContain("concept OrderId : Uuid");
    [Fact] void should_generate_the_non_generic_vogen_concept() => _result.Source.ShouldContain("concept CustomerCode : String");
    [Fact] void should_keep_critter_stack_command_evidence_separate() => _result.Source.ShouldContain("command PlaceOrder");
    [Fact] void should_use_critter_stack_persistence_evidence_for_identity() => _result.Source.ShouldContain("id OrderId");
    [Fact] void should_keep_document_and_unlowered_aggregate_evidence_separate() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(
        "MARTEN0003",
        "GEN0004",
        "GEN0004");
}
