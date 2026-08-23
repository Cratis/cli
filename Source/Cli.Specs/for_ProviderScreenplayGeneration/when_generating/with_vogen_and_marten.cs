// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

/// <summary>
/// Freezes the legacy facade behavior: Vogen is not a selectable provider, but the Marten profile still runs it.
/// </summary>
public class with_vogen_and_marten : given.an_application_scope
{
    GeneratedScreenplay _result = null!;

    async Task Because() => _result = await Generate(
        LoadedFrom(Project("Application", VogenMartenPackages, true, MartenSource, VogenConceptSource)));

    [Fact] void should_report_marten_without_naming_vogen_as_a_provider() => _result.Provenance!.Provider.ShouldEqual(ScreenplayProviders.Marten);
    [Fact] void should_execute_the_hidden_vogen_adapter_inside_the_complete_critter_stack_facade() => _result.Source.ShouldContain("concept OrderId : Uuid");
    [Fact] void should_expose_vogen_only_as_target_evidence() => _result.Provenance!.Projects.Single().Capabilities.ShouldContain("vogen.value-object");
    [Fact] void should_report_the_target_vogen_assembly_identity() => _result.Provenance!.Projects.Single().Assemblies.ShouldContain(new ScreenplayAssemblyIdentity("Vogen", "8.0.7.0"));
}
