// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the outcome of loading a solution or project into the Roslyn compilations to generate from.
/// </summary>
/// <param name="Compilations">The compilations to generate from; empty when nothing could be loaded.</param>
/// <param name="ProjectNames">The names of the projects the compilations came from, in the same order.</param>
/// <param name="Diagnostics">Anything worth reporting about the load itself.</param>
public record LoadedCompilation(IReadOnlyList<Compilation> Compilations, IReadOnlyList<string> ProjectNames, IReadOnlyList<ScreenplayDiagnostic> Diagnostics)
{
    /// <summary>
    /// Gets CLI-owned project, target-framework, package, assembly, and capability provenance in compilation order.
    /// </summary>
    public IReadOnlyList<ScreenplayProjectProvenance> ProjectProvenance { get; init; } = [];

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
            [],
            [],
            [.. warnings ?? [], new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, code, message, location)]);
}
