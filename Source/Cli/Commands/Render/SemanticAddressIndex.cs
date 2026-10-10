// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.Commands.Render;

internal sealed record ArtifactSource(string Id, string Kind, string? Address);

/// <summary>
/// Resolves generated artifact identities to declarations in the current semantic application.
/// </summary>
internal sealed class SemanticAddressIndex
{
    readonly Dictionary<string, ArtifactSource> _sources = new(StringComparer.Ordinal);

    public static SemanticAddressIndex Empty { get; } = new();

    public static SemanticAddressIndex From(SemanticApplication application)
    {
        var index = new SemanticAddressIndex();
        index.Add(application.Id, "application", application.Name);
        foreach (var concept in application.Concepts)
        {
            index.Add(concept.Id, "concept", concept.Name);
        }
        foreach (var type in application.Types)
        {
            index.Add(type.Id, "type", type.Name);
        }
        foreach (var module in application.Modules)
        {
            index.Add(module.Id, "module", module.Name);
            foreach (var feature in module.Features)
            {
                index.AddFeature(feature, module.Name);
            }
        }

        return index;
    }

    public ArtifactSource Resolve(string id) => _sources.GetValueOrDefault(id) ?? new(id, "unknown", null);

    void Add(SemanticId id, string kind, string address) => _sources[id.ToString()] = new(id.ToString(), kind, address);

    void AddFeature(SemanticFeature feature, string parent)
    {
        var address = $"{parent}/{feature.Name}";
        Add(feature.Id, "feature", address);
        foreach (var nested in feature.Features)
        {
            AddFeature(nested, address);
        }
        foreach (var slice in feature.Slices)
        {
            AddSlice(slice, address);
        }
    }

    void AddSlice(SemanticSlice slice, string parent)
    {
        var address = $"{parent}/{slice.Name}";
        Add(slice.Id, "slice", address);
        foreach (var command in slice.Commands)
        {
            Add(command.Id, "command", $"{address}/{command.Name}");
        }
        foreach (var @event in slice.Events)
        {
            Add(@event.Id, "event", $"{address}/{@event.Name}");
        }
        foreach (var readModel in slice.ReadModels)
        {
            Add(readModel.Id, "readmodel", $"{address}/{readModel.Name}");
        }
        foreach (var projection in slice.Projections)
        {
            Add(projection.Id, "projection", $"{address}/{projection.Name}");
        }
        foreach (var query in slice.Queries)
        {
            Add(query.Id, "query", $"{address}/{query.Name}");
        }
        foreach (var specification in slice.Specifications)
        {
            Add(specification.Id, "specification", $"{address}/{specification.Name}");
        }
    }
}
