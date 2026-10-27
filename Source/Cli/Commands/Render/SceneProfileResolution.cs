// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Engine.Packages;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Resolves every <c language="csharp">ui profile</c> of an authored Scene against the packages the bundled Cratis target
/// renders, and refuses the render when a profile asks for something the target cannot supply.
/// </summary>
/// <remarks>
/// <para>
/// The resolution rules are Stage's own <see cref="CratisRendering.ResolveProfileInputs"/>, so the CLI and Studio never hold
/// a second answer to which package satisfies a profile or which layout a profile renders inside. What the CLI owns is the
/// mapping of Stage's <c language="csharp">STAGE-PROFILE-</c> findings to its own diagnostic codes. Screenplay reports a missing layout or an incompatible theme as a warning about source text;
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
    /// The diagnostic code for a profile whose root template cannot be the application's shell: it does not exist, its
    /// blueprint is not installed, its version pin is not satisfied by the installed blueprint or it has no content outlet.
    /// </summary>
    public const string RootTemplateCode = "CLI-RENDER-019";

    /// <summary>
    /// The diagnostic code for a <c language="csharp">template blueprint</c> reference to a blueprint, a version of it or a template that
    /// the approved blueprint does not provide, which would otherwise render a screen with no template.
    /// </summary>
    public const string UnresolvedTemplateCode = "CLI-RENDER-020";

    /// <summary>
    /// Checks every profile of an authored Scene against the bundled target's catalog.
    /// </summary>
    /// <param name="scene">The authored Scene.</param>
    /// <param name="approvedPackages">The custom runtime packages the application approved, or null for none.</param>
    /// <returns>One error for each distinct profile input the target cannot honor; empty when every profile resolves.</returns>
    public static IReadOnlyList<ScreenplayDiagnostic> Check(SceneApplication scene, ApprovedPackageSet? approvedPackages = null)
    {
        return
        [
            .. CratisRendering.ResolveProfileInputs(scene, approvedPackages)
                .Select(finding => (Finding: finding, Code: CodeFor(finding.Code)))
                .Select(_ => new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, _.Code, WithHint(_.Code, _.Finding.Subject, _.Finding.Message), _.Finding.Subject))
        ];
    }

    static string WithHint(string code, string subject, string message) => code switch
    {
        UnknownPackageCode => WithPublishedBlueprintHint($"{message} A custom runtime package has to be approved with --package-set.", subject),
        UnresolvedTemplateCode => WithPublishedBlueprintHint(message, PublishedBlueprintPackage.UnavailableBlueprintIn(message)),
        _ => message
    };

    static string WithPublishedBlueprintHint(string message, string? packageName) =>
        PublishedBlueprintPackage.HintFor(packageName) is { } hint ? $"{message} {hint}" : message;

    static string CodeFor(string stageCode) => stageCode switch
    {
        CratisRendering.UnknownPackageCode => UnknownPackageCode,
        CratisRendering.MissingLayoutCode => MissingLayoutCode,
        CratisRendering.IncompatiblePackageCode => IncompatiblePackageCode,
        CratisRendering.MissingThemeCode => MissingThemeCode,
        CratisRendering.RootTemplateCode => RootTemplateCode,
        CratisRendering.UnresolvedTemplateCode => UnresolvedTemplateCode,
        _ => stageCode
    };
}
