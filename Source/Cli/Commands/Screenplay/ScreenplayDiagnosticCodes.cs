// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// The diagnostic codes the CLI itself reports while preparing a Screenplay generation.
/// </summary>
/// <remarks>
/// These sit alongside the codes the <c>Cratis.Arc.Screenplay</c> generator reports and use a distinct prefix so the
/// two can never be confused for one another.
/// </remarks>
public static class ScreenplayDiagnosticCodes
{
    /// <summary>
    /// No project in the solution could be generated from.
    /// </summary>
    public const string NoProject = "CLI0001";

    /// <summary>
    /// The solution holds more than one candidate project and the choice is ambiguous.
    /// </summary>
    public const string AmbiguousProject = "CLI0002";

    /// <summary>
    /// MSBuild reported a problem while loading the solution or project.
    /// </summary>
    public const string WorkspaceFailure = "CLI0003";

    /// <summary>
    /// The project loaded but Roslyn could not produce a compilation for it.
    /// </summary>
    public const string NoCompilation = "CLI0004";
}
