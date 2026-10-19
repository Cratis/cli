// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the bounds of one scan of an event sequence.
/// </summary>
/// <param name="From">The first sequence number to examine, or <see langword="null"/> to start at the first event.</param>
/// <param name="To">The last sequence number to examine, or <see langword="null"/> to stop at the captured tail.</param>
/// <param name="MaxEvents">The most events to examine; reaching it leaves the rest of the range unexamined.</param>
public sealed record NamespaceObservationScan(ulong? From, ulong? To, int MaxEvents)
{
    /// <summary>
    /// The default number of events one scan examines.
    /// </summary>
    public const int DefaultMaxEvents = 100_000;

    /// <summary>
    /// The number of sequence numbers requested in one metadata read; Chronicle refuses more than 500 locators.
    /// </summary>
    public const int PageSize = 500;
}
