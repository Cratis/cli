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
    readonly Dictionary<string, SemanticId> _identities = new(StringComparer.Ordinal);
    readonly Dictionary<string, IReadOnlyList<string>> _owners = new(StringComparer.Ordinal);
    readonly Dictionary<string, SemanticSlice> _slices = new(StringComparer.Ordinal);

    public static SemanticAddressIndex Empty { get; } = new();

    public IEnumerable<ArtifactSource> Sources => _sources.Values;

    public IEnumerable<SemanticSlice> Slices => _slices.Values;

    public static SemanticAddressIndex From(SemanticApplication application, bool includeRoutes = false)
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
                index.AddFeature(feature, module.Name, [module.Id.ToString()]);
            }
        }

        foreach (var source in includeRoutes ? application.EventSources : [])
        {
            index.Add(source.Id, "eventsource", source.Name);
            foreach (var stream in source.Streams)
            {
                index.Add(stream.Id, "stream", $"{source.Name}/{stream.Name}");
            }
        }

        return index;
    }

    public SemanticId Identity(string id) => _identities[id];

    public IReadOnlyList<string> Owners(string id) => _owners.GetValueOrDefault(id) ?? [];

    public ArtifactSource Resolve(string id) => _sources.GetValueOrDefault(id) ?? new(id, "unknown", null);

    void Add(SemanticId id, string kind, string address)
    {
        _sources[id.ToString()] = new(id.ToString(), kind, address);
        _identities[id.ToString()] = id;
    }

    void AddFeature(SemanticFeature feature, string parent, IReadOnlyList<string> ancestors)
    {
        var address = $"{parent}/{feature.Name}";
        Add(feature.Id, "feature", address);
        var owners = ancestors.Append(feature.Id.ToString()).ToArray();
        _owners[feature.Id.ToString()] = owners;
        foreach (var nested in feature.Features)
        {
            AddFeature(nested, address, owners);
        }
        foreach (var slice in feature.Slices)
        {
            AddSlice(slice, address, owners);
        }
    }

    void AddSlice(SemanticSlice slice, string parent, IReadOnlyList<string> ancestors)
    {
        var address = $"{parent}/{slice.Name}";
        Add(slice.Id, "slice", address);
        _slices[slice.Id.ToString()] = slice;
        var owners = ancestors.Append(slice.Id.ToString()).ToArray();
        _owners[slice.Id.ToString()] = owners;
        foreach (var command in slice.Commands)
        {
            Add(command.Id, "command", $"{address}/{command.Name}");
            _owners[command.Id.ToString()] = owners;
        }
        foreach (var @event in slice.Events)
        {
            Add(@event.Id, "event", $"{address}/{@event.Name}");
            _owners[@event.Id.ToString()] = owners;
        }
        foreach (var readModel in slice.ReadModels)
        {
            Add(readModel.Id, "readmodel", $"{address}/{readModel.Name}");
            _owners[readModel.Id.ToString()] = owners;
        }
        foreach (var projection in slice.Projections)
        {
            Add(projection.Id, "projection", $"{address}/{projection.Name}");
            _owners[projection.Id.ToString()] = owners;
        }
        foreach (var query in slice.Queries)
        {
            Add(query.Id, "query", $"{address}/{query.Name}");
            _owners[query.Id.ToString()] = owners;
        }
        foreach (var specification in slice.Specifications)
        {
            Add(specification.Id, "specification", $"{address}/{specification.Name}");
            _owners[specification.Id.ToString()] = owners;
        }
    }
}
