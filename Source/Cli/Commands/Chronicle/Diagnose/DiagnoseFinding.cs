// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

/// <summary>
/// Identifies a diagnostic finding and the scope it belongs to.
/// </summary>
/// <param name="Check">The check that found the problem or recommendation.</param>
/// <param name="EventStore">The event store.</param>
/// <param name="Namespace">The namespace.</param>
/// <param name="Detail">The observer, partition or recommendation needing attention.</param>
public record DiagnoseFinding(string Check, string EventStore, string Namespace, string Detail);
