// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Checks that the event store, namespace and event sequence of an observation exist before anything is scanned.
/// </summary>
/// <remarks>
/// Chronicle answers a read of a namespace or sequence that does not exist with no events, which would otherwise look
/// like an empty store. Names must match exactly; a name that differs only in case is reported, never chosen.
/// </remarks>
public static class NamespaceTargetCheck
{
    /// <summary>
    /// Finds the first missing part of a target.
    /// </summary>
    /// <param name="observations">The Chronicle reads.</param>
    /// <param name="target">The target.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A message naming what is missing, or <see langword="null"/> when the whole target exists.</returns>
    public static async Task<string?> Missing(INamespaceObservations observations, NamespaceTarget target, CancellationToken cancellationToken)
    {
        if (Absent("Event store", target.EventStore, await observations.EventStores(cancellationToken)) is { } store)
        {
            return store;
        }

        if (Absent("Namespace", target.Namespace, await observations.Namespaces(target.EventStore, cancellationToken)) is { } ns)
        {
            return ns;
        }

        return string.Equals(target.EventSequenceId, CliDefaults.DefaultEventSequenceId, StringComparison.Ordinal)
            ? null
            : Absent("Event sequence", target.EventSequenceId, await observations.EventSequences(target.EventStore, target.Namespace, cancellationToken));
    }

    static string? Absent(string kind, string name, IReadOnlyList<string> existing)
    {
        if (existing.Contains(name, StringComparer.Ordinal))
        {
            return null;
        }

        var similar = existing.Where(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase)).ToArray();
        return similar.Length > 0
            ? $"{kind} '{name}' was not found; names are exact, did you mean {string.Join(" or ", similar.Select(candidate => $"'{candidate}'"))}?"
            : $"{kind} '{name}' was not found";
    }
}
