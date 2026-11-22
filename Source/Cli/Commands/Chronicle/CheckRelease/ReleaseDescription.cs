// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;

namespace Cratis.Cli.Commands.Chronicle.CheckRelease;

/// <summary>
/// What a release tells Chronicle about itself: its wire contract and its event type registrations.
/// </summary>
/// <param name="Release">The release name, for example a tag.</param>
/// <param name="Repository">The optional repository the release belongs to.</param>
/// <param name="Component">The optional component the release belongs to.</param>
/// <param name="Environment">The optional name of the target environment, for the report only.</param>
/// <param name="ClientType">The client type the release identifies as.</param>
/// <param name="ClientVersion">The client version the release was built with.</param>
/// <param name="ProtocolVersion">The wire protocol version the release speaks.</param>
/// <param name="DescriptorSet">The wire contract descriptor set of the release.</param>
/// <param name="MinimumKernelVersion">The optional oldest kernel version the release supports.</param>
/// <param name="EventTypes">The event type registrations the release would make.</param>
public record ReleaseDescription(
    string Release,
    string? Repository,
    string? Component,
    string? Environment,
    string ClientType,
    string ClientVersion,
    uint ProtocolVersion,
    ReadOnlyMemory<byte> DescriptorSet,
    string? MinimumKernelVersion,
    IReadOnlyList<EventTypeRegistration> EventTypes);
