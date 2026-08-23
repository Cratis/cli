// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

/// <summary>
/// Freezes the legacy mismatch: explicit provider selection changes the reported profile, not the complete facade.
/// </summary>
public class with_explicit_marten_and_wolverine_evidence : given.an_application_scope
{
    GeneratedScreenplay _explicitResult = null!;
    GeneratedScreenplay _autoResult = null!;

    async Task Because()
    {
        var loaded = LoadedFrom(Project("Application", CritterStackPackages, false, MartenSource, WolverineSource));
        _explicitResult = await Generate(loaded, ScreenplayProviders.Marten);
        _autoResult = await Generate(loaded);
    }

    [Fact] void should_report_the_explicit_marten_profile() => _explicitResult.Provenance!.Provider.ShouldEqual(ScreenplayProviders.Marten);
    [Fact] void should_keep_wolverine_evidence_under_the_marten_profile() => _explicitResult.Provenance!.Projects.Single().Packages.ShouldContain(new ResolvedScreenplayPackage("WolverineFx", "6.29.1"));
    [Fact] void should_run_the_same_complete_critter_stack_facade_as_auto_selection() => _explicitResult.Source.ShouldEqual(_autoResult.Source);
    [Fact] void should_document_that_auto_selection_reports_a_different_provider_for_the_same_output() => _autoResult.Provenance!.Provider.ShouldEqual(ScreenplayProviders.CritterStack);
}
