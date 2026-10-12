// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Decides which executable semantic model versions <c language="shell">cratis render</c> hands to the bundled renderer.
/// </summary>
/// <remarks>
/// A newer bundled compiler can produce a newer model version than the renderer was built for. Admission is an
/// explicit decision per version, so a package upgrade never lets a model through whose new constructs the
/// renderer would silently drop. Versions newer than the admitted ones are refused here until explicitly
/// reviewed; the renderer diagnoses unsupported constructs within admitted versions. ESM v8 event sources,
/// streams, command routes and specification routes are admitted by the bundled Stage. ESM v9 and v10
/// reach its construct-level admission: unsupported public/foreign events, captures, reaction identity,
/// reaction filters and production route overrides receive typed refusals; reducer filters render. ESM v11
/// exact numbers, v12 enum member numbers and v13 concept compliance are admitted by Stage 4.79.1.
/// ESM v14 query filters remain refused until the renderer admits them.
/// </remarks>
public static class RenderedSemanticVersions
{
    /// <summary>
    /// The diagnostic code reported for a model version <c language="shell">cratis render</c> does not admit.
    /// </summary>
    public const string NotAdmittedCode = "CLI-RENDER-004";

    /// <summary>
    /// Gets the newest model version <c language="shell">cratis render</c> admits.
    /// </summary>
    public static SemanticVersion Newest { get; } = SemanticVersion.V13;

    /// <summary>
    /// Reports a model version <c language="shell">cratis render</c> does not admit.
    /// </summary>
    /// <param name="version">The compiled model's version.</param>
    /// <returns>The blocking diagnostic, or <see langword="null"/> when the version is admitted.</returns>
    public static ScreenplayDiagnostic? Check(SemanticVersion version) => Newest.IsAtLeast(version)
        ? null
        : new(
            ScreenplayDiagnosticSeverity.Error,
            NotAdmittedCode,
            $"The model compiles to ESM v{version}, but 'cratis render' admits ESM v1 through v{Newest}. Newer model versions require explicit renderer admission, so nothing was planned or published.",
            null);
}
