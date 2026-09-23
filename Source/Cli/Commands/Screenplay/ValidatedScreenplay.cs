// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
}
