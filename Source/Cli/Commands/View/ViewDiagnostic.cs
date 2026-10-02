// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Represents something worth telling the user about while preparing a view.
/// </summary>
/// <param name="IsError">Whether this is an error rather than a warning or a note.</param>
/// <param name="Code">The stable code of the diagnostic.</param>
/// <param name="Message">The human readable description.</param>
/// <param name="Location">Where it applies, when known.</param>
public record ViewDiagnostic(bool IsError, string Code, string Message, string? Location)
{
    /// <summary>
    /// Creates a diagnostic from one the CLI reported while loading a project.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to convert.</param>
    /// <returns>The <see cref="ViewDiagnostic"/>.</returns>
    public static ViewDiagnostic From(Screenplay.ScreenplayDiagnostic diagnostic) =>
        new(diagnostic.Severity == Screenplay.ScreenplayDiagnosticSeverity.Error, diagnostic.Code, diagnostic.Message, diagnostic.Location);

    /// <summary>
    /// Creates a diagnostic from one the Arc generator reported.
    /// </summary>
    /// <param name="diagnostic">The diagnostic to convert.</param>
    /// <returns>The <see cref="ViewDiagnostic"/>.</returns>
    public static ViewDiagnostic From(Cratis.Arc.Screenplay.ScreenplayDiagnostic diagnostic) =>
        new(diagnostic.Severity == Cratis.Arc.Screenplay.ScreenplayDiagnosticSeverity.Error, diagnostic.Code, diagnostic.Message, diagnostic.Location);
}
