// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the outcome of compiling one or more Screenplay documents.
/// </summary>
/// <param name="FileCount">The number of <c language="csharp">.play</c> files that were compiled.</param>
/// <param name="Diagnostics">Everything the compiler reported, across every file.</param>
public record ValidatedScreenplay(int FileCount, IReadOnlyList<ScreenplayDiagnostic> Diagnostics)
{
    /// <summary>
    /// Gets the application the compiler produced. A folder of documents is merged into one application before
    /// resolution, so this collection contains at most one entry.
    /// </summary>
    public IReadOnlyList<ApplicationSyntax> Applications { get; init; } = [];

    /// <summary>
    /// Gets whether the documents bind into an executable semantic model, or <see langword="null"/> when binding was not checked.
    /// </summary>
    public bool? Executable { get; init; }

    /// <summary>
    /// Gets the source-scope selection and whole-application counts, or null for unscoped validation.
    /// </summary>
    public ScopedDiagnosticResult? Scoped { get; init; }

    /// <summary>
    /// Gets the selected structural completeness checks.
    /// </summary>
    public CompletenessChecks Checks { get; init; } = CompletenessChecks.None;

    /// <summary>
    /// Gets whether completeness checks ran, were skipped or were not requested.
    /// </summary>
    public string CompletenessStatus { get; init; } = "not requested";

    /// <summary>
    /// Gets the explanation when completeness checks were skipped due to source errors.
    /// </summary>
    public string? CompletenessNote { get; init; }
}
