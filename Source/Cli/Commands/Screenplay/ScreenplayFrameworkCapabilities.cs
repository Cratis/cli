// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Fingerprints source-framework API generations through exact metadata names.
/// </summary>
static class ScreenplayFrameworkCapabilities
{
    static readonly string[] _vogenValueObjectMetadataNames =
    [
        "Vogen.ValueObjectAttribute",
        "Vogen.ValueObjectAttribute`1"
    ];

    static readonly (string Capability, string[] MetadataNames)[] _known =
    [
        ("marten.compiled-query", ["Marten.Linq.ICompiledQuery`2", "Marten.Linq.ICompiledQuery`1"]),
        ("marten.event-projection", ["Marten.Events.Projections.EventProjection"]),
        ("marten.multi-stream-projection", ["Marten.Events.Projections.MultiStreamProjection`2"]),
        ("marten.projection-lifecycle.jasperfx", ["JasperFx.Events.Projections.ProjectionLifecycle"]),
        ("marten.projection-lifecycle.legacy", ["Marten.Events.Projections.ProjectionLifecycle"]),
        ("marten.single-stream-projection.one-identity", ["Marten.Events.Aggregation.SingleStreamProjection`1", "Marten.Events.Projections.SingleStreamProjection`1"]),
        ("marten.single-stream-projection.two-identities", ["Marten.Events.Aggregation.SingleStreamProjection`2", "Marten.Events.Projections.SingleStreamProjection`2"]),
        ("marten.subscription", ["Marten.Subscriptions.ISubscription", "Marten.Subscriptions.SubscriptionBase"]),
        ("wolverine.dcb-model", ["Wolverine.Persistence.EventSourcing.DcbModelAttribute"]),
        ("wolverine.events-to-append", ["Wolverine.Persistence.EventSourcing.EventsToAppend"]),
        ("wolverine.handler-attribute", ["Wolverine.Attributes.WolverineHandlerAttribute"]),
        ("wolverine.legacy-marten-aggregate", ["Wolverine.Marten.AggregateHandlerAttribute", "Wolverine.Marten.WriteAggregateAttribute"]),
        ("wolverine.side-effect", ["Wolverine.ISideEffect"]),
        ("wolverine.store-agnostic-decider", ["Wolverine.Persistence.EventSourcing.DeciderFunctionAttribute"]),
        ("wolverine.store-agnostic-write-model", ["Wolverine.Persistence.EventSourcing.WriteModelAttribute"])
    ];

    /// <summary>
    /// Gets every recognized capability present in the compilation.
    /// </summary>
    /// <param name="compilation">The selected project compilation.</param>
    /// <returns>The stable capability names in deterministic order.</returns>
    public static IReadOnlyList<string> From(Compilation compilation) =>
    [
        .. _known
            .Where(capability => capability.MetadataNames.Any(metadataName => compilation.GetTypeByMetadataName(metadataName) is not null))
            .Select(capability => capability.Capability)
            .Concat(_vogenValueObjectMetadataNames.All(metadataName => compilation.GetTypeByMetadataName(metadataName) is not null)
                ? ["vogen.value-object"]
                : [])
            .Order(StringComparer.Ordinal)
    ];
}
