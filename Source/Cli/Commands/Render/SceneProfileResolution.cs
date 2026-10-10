// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Model.Packages;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Resolves every <c language="csharp">ui profile</c> of an authored Scene against the packages the bundled Cratis target
/// renders, and refuses the render when a profile asks for something the target cannot supply.
/// </summary>
/// <remarks>
/// <para>
/// The resolution rules are Stage's own <see cref="RenderPlanner"/>, used unmodified, so the CLI never holds a second
/// answer to which package satisfies a profile or which layout a profile renders inside. What the CLI owns is the
/// catalog - which packages its bundled target can render - and the decision that a profile input it cannot honor
/// stops the render. Screenplay reports a missing layout or an incompatible theme as a warning about source text;
/// for a render they mean the published application would silently use a different shell or unstyled components,
/// so they are refused before anything is written.
/// </para>
/// <para>
/// Only findings about the profile's own inputs refuse the render. Component, screen-template and size-class
/// findings describe how a valid profile resolves, and the bundled renderer already reports or tolerates those on
/// its own terms.
/// </para>
/// </remarks>
static class SceneProfileResolution
{
    /// <summary>
    /// The diagnostic code for a profile that activates a package the bundled target does not render.
    /// </summary>
    public const string UnknownPackageCode = "CLI-RENDER-007";

    /// <summary>
    /// The diagnostic code for a profile that selects a layout neither the application nor an active package declares.
    /// </summary>
    public const string MissingLayoutCode = "CLI-RENDER-008";

    /// <summary>
    /// The diagnostic code for a profile whose packages cannot be used together: a theme not compatible with an active
    /// package, or a package dependency the catalog cannot satisfy.
    /// </summary>
    public const string IncompatiblePackageCode = "CLI-RENDER-009";

    /// <summary>
    /// The diagnostic code for a profile that selects a theme the application does not declare.
    /// </summary>
    public const string MissingThemeCode = "CLI-RENDER-010";

    /// <summary>
    /// Gets the packages the bundled Cratis target renders.
    /// </summary>
    /// <remarks>
    /// The same catalog Stage plans the canonical screen-composition corpus against for this target: the Scene core
    /// vocabulary, the Scene web components and the Cratis component library the Stage frontend ships.
    /// </remarks>
    public static IReadOnlyList<ScenePackage> Catalog { get; } =
    [
        new("core", "1.0.0", PackageKind.ComponentLibrary, [], [], [], [], [], []),
        new("scene.web", "1.0.0", PackageKind.ComponentLibrary, [], [], [], [], [], []),
        new("Cratis.Components", "4.26.2", PackageKind.ComponentLibrary, [], [], [], [], [], []),
    ];

    /// <summary>
    /// Checks every profile of an authored Scene against the bundled target's catalog.
    /// </summary>
    /// <param name="scene">The authored Scene.</param>
    /// <returns>One error for each distinct profile input the target cannot honor; empty when every profile resolves.</returns>
    public static IReadOnlyList<ScreenplayDiagnostic> Check(SceneApplication scene)
    {
        var plan = RenderPlanner.Plan(scene, Catalog);
        return
        [
            .. plan.Targets
                .SelectMany(target => target.Findings)
                .Concat(plan.Findings)
                .Select(finding => (Finding: finding, Code: CodeFor(finding.Kind)))
                .Where(_ => _.Code is not null)
                .DistinctBy(_ => (_.Finding.Kind, _.Finding.Subject, _.Finding.Message))
                .Select(_ => new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, _.Code!, _.Finding.Message, _.Finding.Subject))
        ];
    }

    static string? CodeFor(RenderFindingKind kind) => kind switch
    {
        RenderFindingKind.PackageNotInCatalog => UnknownPackageCode,
        RenderFindingKind.LayoutNotFound => MissingLayoutCode,
        RenderFindingKind.ThemeIncompatible or
            RenderFindingKind.PackageDependencyMissing or
            RenderFindingKind.PackageVersionConflict or
            RenderFindingKind.PackageDependencyCycle => IncompatiblePackageCode,
        RenderFindingKind.ThemeNotFound => MissingThemeCode,
        _ => null
    };
}
