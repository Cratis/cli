// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents something a Screenplay document does not fully express — a construct the generator could not
/// represent, or a problem the compiler found in a document that already exists.
/// </summary>
/// <param name="Severity">How severe the diagnostic is.</param>
/// <param name="Code">The stable diagnostic code, for example <c>SP0001</c>; empty when the reporting system assigns none.</param>
/// <param name="Message">The human readable description.</param>
/// <param name="Location">The slice, artifact, or file the diagnostic points at; <see langword="null"/> when it applies to the whole document.</param>
public record ScreenplayDiagnostic(
    ScreenplayDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Location)
{
    /// <summary>
    /// Gets the stable semantic subject reported by a typed source diagnostic.
    /// </summary>
    public string? Subject { get; init; }

    /// <summary>
    /// Gets the typed source-diagnostic outcome, such as <c>Conflict</c> or <c>Unsupported</c>.
    /// </summary>
    public string? Outcome { get; init; }
}
