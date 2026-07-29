// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Orders generation diagnostics and turns them into an exit code.
/// </summary>
public static class ScreenplayDiagnostics
{
    /// <summary>
    /// Orders diagnostics by descending severity and then deterministically within each severity.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to order.</param>
    /// <returns>The ordered diagnostics.</returns>
    public static IReadOnlyList<ScreenplayDiagnostic> Order(IEnumerable<ScreenplayDiagnostic> diagnostics) =>
        [.. diagnostics
            .OrderByDescending(diagnostic => diagnostic.Severity)
            .ThenBy(diagnostic => diagnostic.Code, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Location ?? string.Empty, StringComparer.Ordinal)
            .ThenBy(diagnostic => diagnostic.Message, StringComparer.Ordinal)];

    /// <summary>
    /// Groups diagnostics by severity, most severe first, keeping each group deterministically ordered.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to group.</param>
    /// <returns>The groups, most severe first.</returns>
    public static IReadOnlyList<IGrouping<ScreenplayDiagnosticSeverity, ScreenplayDiagnostic>> GroupBySeverity(IEnumerable<ScreenplayDiagnostic> diagnostics) =>
        [.. Order(diagnostics).GroupBy(diagnostic => diagnostic.Severity)];

    /// <summary>
    /// Determines whether any diagnostic is an error.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to inspect.</param>
    /// <returns><see langword="true"/> when at least one diagnostic is an error.</returns>
    public static bool HasErrors(IEnumerable<ScreenplayDiagnostic> diagnostics) =>
        diagnostics.Any(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error);

    /// <summary>
    /// Resolves the exit code for a set of diagnostics — non-zero as soon as one of them is an error.
    /// </summary>
    /// <param name="diagnostics">The diagnostics to inspect.</param>
    /// <returns>The exit code.</returns>
    public static int ExitCodeFor(IEnumerable<ScreenplayDiagnostic> diagnostics) =>
        HasErrors(diagnostics) ? ExitCodes.ValidationError : ExitCodes.Success;
}
