// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

/// <summary>
/// Identifies a diagnostic check that could not run.
/// </summary>
/// <param name="Check">The check name.</param>
/// <param name="EventStore">The event store, or null for server-wide checks.</param>
/// <param name="Namespace">The namespace, or null for store-wide checks.</param>
/// <param name="Reason">The reported exception message.</param>
public record DiagnoseCheckFailure(string Check, string? EventStore, string? Namespace, string Reason);
