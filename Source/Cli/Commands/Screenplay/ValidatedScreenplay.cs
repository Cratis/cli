// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the outcome of compiling one or more Screenplay documents.
/// </summary>
/// <param name="FileCount">The number of <c>.play</c> files that were compiled.</param>
/// <param name="Diagnostics">Everything the compiler reported, across every file.</param>
public record ValidatedScreenplay(int FileCount, IReadOnlyList<ScreenplayDiagnostic> Diagnostics);
