// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.CritterStack.Screenplay;
using Cratis.Screenplay.Generation;
using Cratis.Screenplay.Generation.DotNet.Vogen;

namespace Cratis.Cli.for_ProviderScreenplayGeneration.when_generating;

/// <summary>
/// Reads the complete facade result directly because the CLI facade discards its resolved graph.
/// </summary>
public class with_vogen_and_critter_stack_resolved_evidence : given.an_application_scope
{
    GeneratedScreenplayDefinition _result = null!;
    AdapterIdentity[] _adapterIdentities = [];

    void Because()
    {
        _result = GenerateWithCritterStackFacade(
            LoadedFrom(Project(
                "Application",
                VogenCritterStackPackages,
                true,
                MartenSource,
                WolverineSource,
                VogenConceptSource)));
        _adapterIdentities =
        [
            .. _result.Graph.Artifacts
                .SelectMany(artifact => artifact.Variants)
                .SelectMany(variant => variant.Evidence)
                .Concat(_result.Graph.ConceptRepresentations
                    .SelectMany(representation => representation.Variants)
                    .SelectMany(variant => variant.Evidence))
                .Select(evidence => evidence.Adapter)
                .Distinct()
        ];
    }

    [Fact] void should_resolve_evidence_from_the_critter_stack_adapter() => _adapterIdentities.ShouldContain(new CritterStackScreenplayAdapter().Identity);
    [Fact] void should_resolve_evidence_from_the_vogen_adapter() => _adapterIdentities.ShouldContain(new VogenConceptScreenplayAdapter().Identity);
    [Fact] void should_expose_the_resolved_graph_only_from_the_complete_facade_result() => _result.Graph.Artifacts.ShouldNotBeEmpty();
}
