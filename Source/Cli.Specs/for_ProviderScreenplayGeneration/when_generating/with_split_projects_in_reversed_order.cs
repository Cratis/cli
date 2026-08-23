// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

/// <summary>
/// Characterizes the stable semantic output and the legacy loader-order project reporting separately.
/// </summary>
public class with_split_projects_in_reversed_order : given.an_application_scope
{
    GeneratedScreenplay _forward = null!;
    GeneratedScreenplay _reversed = null!;

    async Task Because()
    {
        var persistence = Project(
            "Persistence",
            [new ResolvedScreenplayPackage("Marten", "9.23.0"), new ResolvedScreenplayPackage("WolverineFx.Marten", "6.29.1")],
            false,
            MartenSource);
        var worker = Project("Worker", [new ResolvedScreenplayPackage("WolverineFx", "6.29.1")], false, WolverineSource);
        _forward = await Generate(LoadedFrom(persistence, worker));
        _reversed = await Generate(LoadedFrom(worker, persistence));
    }

    [Fact] void should_select_critter_stack_in_both_orders() => _reversed.Provenance!.Provider.ShouldEqual(_forward.Provenance!.Provider);
    [Fact] void should_generate_identical_semantic_output_in_both_orders() => _reversed.Source.ShouldEqual(_forward.Source);
    [Fact] void should_preserve_loader_order_in_reported_projects_as_current_legacy_behavior() => _reversed.Projects.ShouldContainOnly(["Worker", "Persistence"]);
}
