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

    /// <summary>
    /// The requested source framework provider is not recognized.
    /// </summary>
    public const string InvalidProvider = "CLI0007";

    /// <summary>
    /// Source compilation errors make the recovered semantic model untrustworthy.
    /// </summary>
    public const string SourceDidNotCompile = "CLI0008";

    /// <summary>
    /// A solution contains several deployable hosts and therefore does not identify one application.
    /// </summary>
    public const string AmbiguousApplicationHosts = "CLI0009";

    /// <summary>
    /// No bundled source provider recognizes the loaded application.
    /// </summary>
    public const string NoMatchingProvider = "CLI0010";

    /// <summary>
    /// More than one unrelated source provider recognizes the loaded application.
    /// </summary>
    public const string AmbiguousProviders = "CLI0011";

    /// <summary>
    /// Resolved package provenance is insufficient to classify the recognized framework generation safely.
    /// </summary>
    public const string UnknownFrameworkVersion = "CLI0012";

    /// <summary>
    /// A resolved framework major generation is newer than or outside the source-reviewed compatibility baseline.
    /// </summary>
    public const string UnsupportedFrameworkVersion = "CLI0013";

    /// <summary>
    /// A provider does not support a requested generation option and therefore leaves it unapplied.
    /// </summary>
    public const string UnsupportedGenerationOption = "CLI0014";

    /// <summary>
    /// A project targets several frameworks and no target framework was requested.
    /// </summary>
    public const string AmbiguousTargetFramework = "CLI0015";

    /// <summary>
    /// A requested target framework is not available for a multi-targeted project.
    /// </summary>
    public const string UnavailableTargetFramework = "CLI0016";
}
