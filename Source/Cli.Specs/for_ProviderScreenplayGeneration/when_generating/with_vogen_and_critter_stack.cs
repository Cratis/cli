// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

public class with_vogen_and_critter_stack : given.an_application_scope
{
    GeneratedScreenplay _result = null!;

    async Task Because() => _result = await Generate(
        LoadedFrom(Project(
            "Application",
            VogenCritterStackPackages,
            true,
            MartenSource,
            WolverineSource,
            VogenConceptSource)));

    [Fact] void should_report_critter_stack() => _result.Provenance!.Provider.ShouldEqual(ScreenplayProviders.CritterStack);
    [Fact] void should_execute_the_vogen_part_of_the_complete_facade() => _result.Source.ShouldContain("concept OrderId : Uuid");
    [Fact] void should_execute_the_marten_part_of_the_complete_facade() => _result.Source.ShouldContain("reducer AccountSnapshot => Account");
    [Fact] void should_keep_vogen_visible_as_resolved_application_evidence() => _result.Provenance!.Projects.Single().Packages.ShouldContain(new ResolvedScreenplayPackage("Vogen", "8.0.7"));
}
