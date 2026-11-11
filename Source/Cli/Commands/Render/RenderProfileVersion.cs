// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Xml.Linq;
using Cratis.Cli.Commands.Render.Publication;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Chooses the Stage scaffold profile version a Cratis render uses, without changing application files.
/// </summary>
/// <remarks>
/// A new application gets the newest profile ("4", .NET 11). An existing application keeps the profile it was rendered with,
/// read from the TargetFramework of its managed Directory.Build.props; an unrecorded one is profile "3" (.NET 10).
/// Moving an existing application to the newest profile needs the explicit upgrade opt-in.
/// </remarks>
internal static class RenderProfileVersion
{
    /// <summary>The first Stage scaffold profile, which targets .NET 10.</summary>
    public const string Net10 = "3";

    /// <summary>The Stage scaffold profile that targets .NET 11.</summary>
    public const string Net11 = "4";

    /// <summary>The code of the diagnostic reported when the destination's ownership manifest cannot be read.</summary>
    public const string UnreadableCode = "CLI-RENDER-033";

    /// <summary>The code of the diagnostic reported when the upgrade opt-in is used with a target that has no scaffold profiles.</summary>
    public const string UpgradeNotSupportedCode = "CLI-RENDER-034";

    /// <summary>The code of the diagnostic reported when the upgrade opt-in is combined with a scoped render.</summary>
    public const string UpgradeScopedCode = "CLI-RENDER-035";

    const string ScaffoldProperties = "Directory.Build.props";

    /// <summary>
    /// Gets the diagnostic that refuses the upgrade opt-in when it cannot take effect, or null when it can.
    /// </summary>
    /// <param name="upgrade">Whether the application opted in to the newest profile.</param>
    /// <param name="hasScaffoldProfiles">Whether the target renders a versioned scaffold.</param>
    /// <param name="scoped">Whether the render is limited to a scope.</param>
    /// <returns>The blocking diagnostic, or null.</returns>
    public static ScreenplayDiagnostic? UpgradeRefusal(bool upgrade, bool hasScaffoldProfiles, bool scoped) => (upgrade, hasScaffoldProfiles, scoped) switch
    {
        (true, false, _) => Refusal(UpgradeNotSupportedCode, "The selected target does not support --upgrade-profile; use the 'cratis' target."),
        (true, true, true) => Refusal(UpgradeScopedCode, "--upgrade-profile cannot be combined with --scope because a scoped render does not update the application scaffold; run an unscoped render to upgrade."),
        _ => null
    };

    /// <summary>
    /// Resolves the scaffold profile version for a destination.
    /// </summary>
    /// <param name="destination">The publication destination, or null for destination-independent planning.</param>
    /// <param name="upgrade">Whether the application opted in to the newest profile.</param>
    /// <returns>The version, or null for the Stage default; or a blocking diagnostic.</returns>
    public static (string? Version, ScreenplayDiagnostic? Error) Resolve(string? destination, bool upgrade)
    {
        if (destination is null)
        {
            return (null, null);
        }

        try
        {
            ArtifactPublicationStorage.EnsureSafePath(destination, ArtifactPublicationStorage.ManifestFileName);
            var manifest = ArtifactPublicationStorage.ReadManifest(destination);
            return (VersionFor(destination, manifest, upgrade), null);
        }
        catch (Exception exception) when (exception is InvalidArtifactRenderContract or IOException or UnauthorizedAccessException or UnsafeArtifactPublication)
        {
            return (null, new(ScreenplayDiagnosticSeverity.Error, UnreadableCode, $"The destination's ownership manifest cannot be read, so its scaffold profile cannot be determined: {exception.Message} Restore the original files for this application and render again.", null));
        }
    }

    static ScreenplayDiagnostic Refusal(string code, string message) => new(ScreenplayDiagnosticSeverity.Error, code, message, null);

    static string VersionFor(string destination, ArtifactManifest? manifest, bool upgrade) =>
        upgrade || IsNewApplication(destination, manifest) || IsRecordedAsNet11(destination, manifest) ? Net11 : Net10;

    static bool IsNewApplication(string destination, ArtifactManifest? manifest) =>
        manifest is null && !File.Exists(ArtifactPublicationStorage.ArtifactPath(destination, ScaffoldProperties));

    static bool IsRecordedAsNet11(string destination, ArtifactManifest? manifest) =>
        manifest is not null && RecordedTargetFramework(destination, manifest) == "net11.0";

    static string? RecordedTargetFramework(string destination, ArtifactManifest manifest)
    {
        var owned = manifest.Artifacts
            .Where(artifact => string.Equals(Path.GetFileName(artifact.Path), ScaffoldProperties, StringComparison.Ordinal))
            .OrderBy(artifact => artifact.Path.Count(character => character == '/'))
            .ThenBy(artifact => artifact.Path, StringComparer.Ordinal)
            .FirstOrDefault();
        if (owned is null)
        {
            return null;
        }

        ArtifactPublicationStorage.EnsureSafePath(destination, owned.Path);
        var path = ArtifactPublicationStorage.ArtifactPath(destination, owned.Path);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            return XDocument.Parse(File.ReadAllText(path)).Descendants("TargetFramework").FirstOrDefault()?.Value.Trim();
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }
    }
}
