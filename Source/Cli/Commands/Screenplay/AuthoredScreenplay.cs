// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// The complete source documents and source diagnostics of an authored application.
/// </summary>
/// <param name="ApplicationName">The authored domain name, or the unnamed application fallback.</param>
/// <param name="Sources">Portable document paths and complete text, including imports.</param>
/// <param name="Diagnostics">Source compilation diagnostics (not executable binding diagnostics).</param>
public sealed record AuthoredScreenplay(string ApplicationName, IReadOnlyDictionary<string, string> Sources, IReadOnlyList<ScreenplayDiagnostic> Diagnostics);
