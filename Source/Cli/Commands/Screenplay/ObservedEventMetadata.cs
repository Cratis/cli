// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Observations;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the parts of one appended event's metadata a namespace observation needs.
/// </summary>
/// <remarks>
/// Deliberately carries no payload, event source identity, subject or causation property values, so nothing else can
/// leak them into a snapshot or a report.
/// </remarks>
/// <param name="SequenceNumber">The event's sequence number.</param>
/// <param name="EventTypeId">The stored event type identifier.</param>
/// <param name="Producer">The innermost recognized producer, or <see langword="null"/> when the causation names none.</param>
public sealed record ObservedEventMetadata(ulong SequenceNumber, string EventTypeId, ObservedProducer? Producer);
