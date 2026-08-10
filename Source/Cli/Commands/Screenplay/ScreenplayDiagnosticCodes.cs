// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// The diagnostic codes the CLI itself reports while preparing a Screenplay generation.
/// </summary>
/// <remarks>
/// These sit alongside the codes the <c>Cratis.Arc.Screenplay</c> generator reports and use a distinct prefix so the
/// two can never be confused for one another.
/// <para>
/// <c>CLI0002</c> is retired and must not be reused. It reported a solution holding more than one candidate project
/// as ambiguous, which stopped being a question once several projects could describe one application together.
/// </para>
/// </remarks>
public static class ScreenplayDiagnosticCodes
{
    /// <summary>
    /// No project in the solution could be generated from.
    /// </summary>
    public const string NoProject = "CLI0001";

    /// <summary>
    /// MSBuild reported a problem while loading the solution or project.
    /// </summary>
    public const string WorkspaceFailure = "CLI0003";

    /// <summary>
    /// A project loaded but Roslyn could not produce a compilation for it, so it is not part of the document.
    /// </summary>
    public const string NoCompilation = "CLI0004";

    /// <summary>
    /// A project has not been restored, so nothing it references can be resolved.
    /// </summary>
    public const string RestoreRequired = "CLI0005";

    /// <summary>
    /// Every project loaded, and none of them can declare anything a Screenplay document is made of.
    /// </summary>
    public const string NoArtifacts = "CLI0006";
}
