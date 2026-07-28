// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents something the generator could not fully express in the generated Screenplay document.
/// </summary>
/// <param name="Severity">How severe the diagnostic is.</param>
/// <param name="Code">The stable diagnostic code, for example <c>SP0001</c>.</param>
/// <param name="Message">The human readable description.</param>
/// <param name="Location">The slice, artifact, or file the diagnostic points at; <see langword="null"/> when it applies to the whole document.</param>
public record ScreenplayDiagnostic(
    ScreenplayDiagnosticSeverity Severity,
    string Code,
    string Message,
    string? Location);
