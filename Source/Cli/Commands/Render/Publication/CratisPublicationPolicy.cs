// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render.Publication;

/// <summary>Adapts Cratis-specific compatibility checks to shared publication refusals.</summary>
internal static class CratisPublicationPolicy
{
    internal static RenderPublicationRefusal? Check(ArtifactRenderPlan plan, Func<string, string?> readExisting) =>
        CratisRendering.CheckPublication(plan, readExisting) switch
        {
            CratisPublicationCheck.Compatible => null,
            CratisPublicationCheck.RequiresApplicationScope required => new(
                $"Incompatible policy layout: {string.Join(", ", required.Paths)}. Render the whole application once before rendering a scope.",
                "CLI-RENDER-013"),
            CratisPublicationCheck.UnsafeRemovals removals => new(
                $"Unsafe reducer migration removals: {string.Join("; ", removals.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"))}",
                ArtifactPublicationRemovals.RefusalCode),
            CratisPublicationCheck.ChangesEventTypeIds eventIds => new(EventTypeIdMessage(eventIds), "CLI-RENDER-016"),
            _ => new("The Stage publication check did not confirm compatibility.")
        };

    static string EventTypeIdMessage(CratisPublicationCheck.ChangesEventTypeIds eventIds) => string.Join(' ', new[]
    {
        $"Stored event type ids would change or could not be determined: {string.Join("; ", eventIds.Changes.Select(change => $"{change.Path}: {change.TypeName}: {change.ExistingId} -> {change.PlannedId ?? "<unknown>"}"))}.",
        string.Join("; ", eventIds.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")),
        "Pin the event's 'id' to keep its stored event type id."
    }.Where(part => part.Length > 0));
}
