// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the outcome of generating a Screenplay document from source code.
/// </summary>
/// <param name="Source">The generated <c>.play</c> source; empty when generation failed outright.</param>
/// <param name="Diagnostics">Everything the generator could not fully express, in the order it was reported.</param>
public record GeneratedScreenplay(string Source, IReadOnlyList<ScreenplayDiagnostic> Diagnostics)
{
    /// <summary>
    /// Gets the names of the projects the document was generated from.
    /// </summary>
    /// <remarks>
    /// An application is regularly split across several projects, so which ones took part is the difference between
    /// a document that describes all of it and one that describes half of it.
    /// </remarks>
    public IReadOnlyList<string> Projects { get; init; } = [];

    /// <summary>
    /// Gets an outcome carrying no source and a single error diagnostic.
    /// </summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="message">The human readable description.</param>
    /// <returns>The failed <see cref="GeneratedScreenplay"/>.</returns>
    public static GeneratedScreenplay Failed(string code, string message) =>
        new(string.Empty, [new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, code, message, null)]);
}
