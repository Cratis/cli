// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Packages;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// What a live Stage is handed so it can load the custom runtime packages of an approved package set: the set itself and the
/// browser build of each package in it, each mounted read-only.
/// </summary>
/// <param name="SetFile">The full path of the approved package set document.</param>
/// <param name="Packages">The npm name and full path of the folder of each approved package.</param>
public sealed record StagePackageMounts(string SetFile, IReadOnlyList<(string Npm, string Folder)> Packages)
{
    /// <summary>
    /// The path inside the container the set and the packages are mounted under. It is the folder the Stage reads its packages from.
    /// </summary>
    public const string MountPath = "/packages";

    /// <summary>
    /// The path inside the container the set document is mounted at.
    /// </summary>
    public const string SetMountPath = MountPath + "/approved-packages.json";

    /// <summary>
    /// Resolves the folders the packages of a set are served from.
    /// </summary>
    /// <param name="setFile">The full path of the set document.</param>
    /// <param name="set">The approved package set.</param>
    /// <param name="packagesFolder">The folder holding a folder for each package, named by its npm name, or null for the folder the set is in.</param>
    /// <returns>The mounts, or the reason a package cannot be mounted.</returns>
    /// <remarks>
    /// A package is a browser ES module: its folder holds the module its <c language="json">package.json</c> names
    /// (<c language="json">exports</c>, <c language="json">module</c> or <c language="json">main</c>, or <c language="shell">index.js</c>) and the style sheets the set lists.
    /// Only those folders are mounted, never the folder that holds them. Docker's <c language="shell">-v</c> option separates the
    /// host path from the container path with a colon, so a path containing one is refused.
    /// </remarks>
    public static (StagePackageMounts? Mounts, string? Error) Resolve(string setFile, ApprovedPackageSet set, string? packagesFolder)
    {
        var root = Path.GetFullPath(packagesFolder ?? Path.GetDirectoryName(setFile) ?? ".");
        if (!Directory.Exists(root))
        {
            return (null, $"The packages folder '{root}' does not exist");
        }

        var packages = new List<(string Npm, string Folder)>();
        foreach (var package in set.Packages)
        {
            var folder = Path.GetFullPath(Path.Combine(root, package.Module.Npm));
            if (!folder.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            {
                return (null, $"The package '{package.Module.Npm}' is not inside the packages folder");
            }

            if (!Directory.Exists(folder))
            {
                return (null, $"The approved package '{package.Manifest.Name}' has no browser build: the folder '{folder}' does not exist. A live Stage loads the package from a folder named by its npm name '{package.Module.Npm}' inside the packages folder");
            }

            packages.Add((package.Module.Npm, folder));
        }

        foreach (var path in packages.Select(_ => _.Folder).Append(setFile))
        {
            var withoutDrive = OperatingSystem.IsWindows() && path.Length > 2 && path[1] == ':' ? path[2..] : path;
            if (withoutDrive.Contains(':', StringComparison.Ordinal))
            {
                return (null, $"'{path}' contains a colon, which Docker's -v option cannot represent");
            }
        }

        return (new(setFile, packages), null);
    }

    /// <summary>
    /// Builds the <c language="shell">docker run</c> arguments that mount the set and the packages read-only.
    /// </summary>
    /// <returns>The ordered arguments.</returns>
    public IReadOnlyList<string> BuildVolumeArguments() =>
    [
        "-v",
        $"{SetFile}:{SetMountPath}:ro",
        .. Packages.SelectMany(package => new[] { "-v", $"{package.Folder}:{MountPath}/{package.Npm}:ro" })
    ];
}
