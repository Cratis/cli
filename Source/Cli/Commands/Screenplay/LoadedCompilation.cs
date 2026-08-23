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
    /// Gets the syntax trees known to come from authored project documents in compilation order.
    /// </summary>
    public IReadOnlyList<IReadOnlySet<SyntaxTree>> AuthoredSyntaxTrees { get; init; } = [];

    /// <summary>
    /// Gets CLI-owned project, target-framework, package, assembly, and capability provenance in compilation order.
    /// </summary>
    public IReadOnlyList<ScreenplayProjectProvenance> ProjectProvenance { get; init; } = [];

    /// <summary>
    /// Gets physical and relocation-safe logical source metadata in compilation order.
    /// </summary>
    public IReadOnlyList<ScreenplayProjectSource> ProjectSources { get; init; } = [];

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

    /// <summary>
    /// Gets the fail-closed diagnostic when project source metadata is not aligned with the compilations it describes.
    /// </summary>
    /// <param name="location">The solution or project the failure applies to.</param>
    /// <returns>The alignment diagnostic, or <see langword="null"/> when metadata is absent or exactly aligned.</returns>
    internal ScreenplayDiagnostic? ProjectSourceAlignmentFailureFor(string? location) =>
        ProjectSources.Count is 0 || ProjectSources.Count == Compilations.Count
            ? null
            : new ScreenplayDiagnostic(
                ScreenplayDiagnosticSeverity.Error,
                ScreenplayDiagnosticCodes.InvalidSourceMetadata,
                $"Project source metadata contains {ProjectSources.Count} entries for {Compilations.Count} compilations; it must be empty or contain exactly one entry per compilation",
                location);

    /// <summary>
    /// Gets a fail-closed generation result when project source metadata is not aligned.
    /// </summary>
    /// <param name="location">The solution or project the failure applies to.</param>
    /// <returns>The failed generation result, or <see langword="null"/> when metadata is absent or exactly aligned.</returns>
    internal GeneratedScreenplay? ProjectSourceAlignmentFailureResultFor(string location) =>
        ProjectSourceAlignmentFailureFor(location) is { } diagnostic
            ? new GeneratedScreenplay(string.Empty, [.. Diagnostics, diagnostic]) { Projects = ProjectNames }
            : null;
}
