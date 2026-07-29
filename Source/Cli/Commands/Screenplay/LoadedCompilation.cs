// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the outcome of loading a solution or project into a Roslyn compilation.
/// </summary>
/// <param name="Compilation">The compilation to generate from; <see langword="null"/> when loading failed.</param>
/// <param name="ProjectName">The name of the project the compilation came from; empty when loading failed.</param>
/// <param name="Diagnostics">Anything worth reporting about the load itself.</param>
public record LoadedCompilation(Compilation? Compilation, string ProjectName, IReadOnlyList<ScreenplayDiagnostic> Diagnostics)
{
    /// <summary>
    /// Gets a failed outcome carrying a single error diagnostic.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The human readable description.</param>
    /// <param name="location">The solution or project the failure applies to.</param>
    /// <param name="warnings">Diagnostics gathered before the failure.</param>
    /// <returns>The failed <see cref="LoadedCompilation"/>.</returns>
    public static LoadedCompilation Failed(string code, string message, string? location, IEnumerable<ScreenplayDiagnostic>? warnings = null) =>
        new(
            null,
            string.Empty,
            [.. warnings ?? [], new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, code, message, location)]);
}
