// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Enumerates declaration references in admitted slices, including their generated specifications.
/// </summary>
internal static class RenderDependencies
{
    static readonly string[] _declarationKinds = ["event", "readmodel", "command", "query", "eventsource", "stream"];

    public static IReadOnlyList<ArtifactSource> Collect(RenderSelection selection, SemanticAddressIndex index)
    {
        var selected = index.Slices.Where(slice => selection.Contains(slice.Id.ToString(), index)).ToArray();
        var produced = selected.SelectMany(slice => slice.Commands).SelectMany(command => command.Produces)
            .Select(production => production.EventContract).ToHashSet();
        var constraints = index.Slices.SelectMany(slice => slice.Constraints).Where(constraint => constraint.Targets.Any(target => produced.Contains(target.EventContract)))
            .SelectMany(constraint => constraint.Targets.Select(target => target.EventContract).Concat(constraint.ReleasedBy));

        return [.. selected.SelectMany(References).Concat(constraints)
            .Select(id => index.Resolve(id.ToString()))
            .Where(source => _declarationKinds.Contains(source.Kind, StringComparer.Ordinal) && !selection.Contains(source.Id, index)).Distinct()
            .OrderBy(source => source.Address, StringComparer.Ordinal).ThenBy(source => source.Id, StringComparer.Ordinal)];
    }

    static IEnumerable<SemanticId> References(SemanticSlice slice) =>
        slice.Commands.SelectMany(command => command.Produces.Select(produced => produced.EventContract).Concat(Route(command.Route)))
            .Concat(slice.Queries.Select(query => query.ReadModel))
            .Concat(slice.Projections.SelectMany(projection => new[] { projection.ReadModel }
                .Concat(projection.Transitions.Select(transition => transition.EventContract)).Concat(Scope(projection.Scope))))
            .Concat(slice.Reducers.SelectMany(reducer => new[] { reducer.ReadModel }.Concat(reducer.Transitions.Select(transition => transition.EventContract))))
            .Concat(slice.Constraints.SelectMany(constraint => constraint.Targets.Select(target => target.EventContract).Concat(constraint.ReleasedBy)))
            .Concat(slice.Specifications.SelectMany(Specification));

    static IEnumerable<SemanticId> Route(SemanticCommandRoute? route) => route is null ? [] : [route.Source, route.Stream];

    static IEnumerable<SemanticId> FixtureRoute(SemanticFixtureRoute? route) => route is null ? [] : [route.Source, route.Stream];

    static IEnumerable<SemanticId> Scope(SemanticProjectionScope? scope) => scope is null ? [] :
        scope.From.Select(from => from.EventContract).Concat(scope.Joins.Select(join => join.EventContract))
            .Concat(scope.Removals.Select(removal => removal.EventContract)).Concat(scope.JoinRemovals.Select(removal => removal.EventContract))
            .Concat(scope.Children.SelectMany(child => Scope(child.Scope))).Concat(scope.Nested.SelectMany(nested => Scope(nested.Scope)));

    static IEnumerable<SemanticId> Specification(SemanticSpecification specification) =>
        specification.GivenEvents.Concat(specification.ThenEvents).SelectMany(@event => new[] { @event.EventContract }.Concat(FixtureRoute(@event.Route)))
            .Concat(specification.GivenReadModels.Concat(specification.ThenReadModels).Select(model => model.ReadModel))
            .Concat(specification.ThenAbsentReadModels.Select(model => model.ReadModel))
            .Concat(specification.ThenQueries.SelectMany(query => new[] { query.Query }.Concat(query.Results.Select(model => model.ReadModel))))
            .Concat(specification.When is { } command ? [command.Command] : [])
            .Concat(specification.WhenAppended is { } append ? new[] { append.EventContract }.Concat(FixtureRoute(append.Route)) : []);
}
