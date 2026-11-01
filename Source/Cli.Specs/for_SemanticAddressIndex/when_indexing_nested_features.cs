// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_SemanticAddressIndex;

public class when_indexing_nested_features : for_ScreenplayPlanning.given.a_canonical_screenplay
{
    SemanticAddressIndex _index = null!;
    SemanticApplication _application = null!;
    SemanticModule _module = null!;
    SemanticFeature _feature = null!;

    async Task Establish()
    {
        await _planning.Plan(new(WriteSource("single"), Corpus.ApplicationName, "cratis"), CancellationToken.None);
        _application = _requests.Single().Model.Application;
        _module = _application.Modules.Single();
        _feature = _module.Features.Single();
        _application = _application with
        {
            Types = [new(SemanticId.Create(SemanticKind.CompositeType, "details"), "Details", [])],
            Modules = [_module with { Features = [new(SemanticId.Create(SemanticKind.Feature, "parent"), "Parent", [_feature], [])] }]
        };
    }

    void Because() => _index = SemanticAddressIndex.From(_application);

    [Fact] void should_resolve_the_application() => _index.Resolve(_application.Id.ToString()).ShouldEqual(new ArtifactSource(_application.Id.ToString(), "application", _application.Name));
    [Fact] void should_resolve_the_module() => _index.Resolve(_module.Id.ToString()).Address.ShouldEqual("Projects");
    [Fact] void should_include_every_parent_feature() => _index.Resolve(_feature.Id.ToString()).Address.ShouldEqual("Projects/Parent/Registration");
    [Fact] void should_resolve_the_nested_slice() => _index.Resolve(_feature.Slices[0].Id.ToString()).Address.ShouldEqual($"Projects/Parent/Registration/{_feature.Slices[0].Name}");
    [Fact] void should_resolve_application_concepts_without_a_prefix() => _index.Resolve(_application.Concepts[0].Id.ToString()).Address.ShouldEqual(_application.Concepts[0].Name);
    [Fact] void should_resolve_application_types_without_a_prefix() => _index.Resolve(_application.Types.Single().Id.ToString()).ShouldEqual(new ArtifactSource(_application.Types.Single().Id.ToString(), "type", "Details"));
    [Fact] void should_resolve_nested_queries() => AssertDeclaration("query", "ProjectById", "Projects/Parent/Registration/ProjectLookup/ProjectById");
    [Fact] void should_resolve_nested_projections() => AssertDeclaration("projection", "ProjectSummaryProjection", "Projects/Parent/Registration/ProjectLookup/ProjectSummaryProjection");

    void AssertDeclaration(string kind, string name, string address)
    {
        var id = kind == "query"
            ? _feature.Slices.SelectMany(slice => slice.Queries).Single(query => query.Name == name).Id
            : _feature.Slices.SelectMany(slice => slice.Projections).Single(projection => projection.Name == name).Id;
        _index.Resolve(id.ToString()).ShouldEqual(new ArtifactSource(id.ToString(), kind, address));
    }
}
