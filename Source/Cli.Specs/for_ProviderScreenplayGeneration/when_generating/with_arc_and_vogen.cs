// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

/// <summary>
/// Freezes the legacy omission: Arc selection retains Vogen provenance but its facade cannot compose Vogen facts.
/// </summary>
public class with_arc_and_vogen : given.an_application_scope
{
    GeneratedScreenplay _result = null!;

    async Task Because() => _result = await Generate(
        LoadedFrom(Project(
            "Application",
            [new ResolvedScreenplayPackage("Vogen", "8.0.7")],
            true,
            ArcSource,
            VogenConceptSource)));

    [Fact] void should_report_arc() => _result.Provenance!.Provider.ShouldEqual(ScreenplayProviders.Arc);
    [Fact] void should_execute_the_arc_facade() => _result.Source.ShouldContain("command PlaceOrder");
    [Fact] void should_omit_the_cross_cutting_vogen_concept_as_current_legacy_behavior() => _result.Source.ShouldNotContain("concept OrderId");
    [Fact] void should_retain_vogen_capability_evidence_even_though_generation_omits_it() => _result.Provenance!.Projects.Single().Capabilities.ShouldContain("vogen.value-object");
}
