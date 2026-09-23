// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;

namespace Cratis.Templating.Packages;

/// <summary>
/// One template discovered inside an acquired package: its parsed manifest and the directory holding
/// its source files.
/// </summary>
/// <param name="Manifest">The parsed manifest.</param>
/// <param name="Directory">The template's root directory — the one containing .template.config.</param>
public record DiscoveredTemplate(TemplateConfig Manifest, string Directory);

/// <summary>
/// Acquires template packages into the CLI's own template store and discovers templates in them, by
/// scanning for <c language="csharp">.template.config/template.json</c> at any depth. The store is isolated from the
/// dotnet template store — nothing here reads or writes <c language="csharp">~/.dotnet</c>. Cached packages resolve offline.
/// </summary>
/// <param name="storeRoot">The root directory packages are cached under.</param>
/// <param name="client">The NuGet client to acquire packages with.</param>
public class TemplatePackageStore(string storeRoot, NuGetClient? client = null)
{
    readonly NuGetClient _client = client ?? new NuGetClient();

    /// <summary>
    /// Gets the root directory packages are cached under.
    /// </summary>
    public string StoreRoot { get; } = storeRoot;

    /// <summary>
    /// Discovers every template in a package directory by scanning for .template.config/template.json
    /// at any depth, so packages produced for any language work identically.
    /// </summary>
    /// <param name="packageRoot">The package root directory.</param>
    /// <returns>The discovered templates.</returns>
    /// <exception cref="TemplatePackageAcquisitionError">Thrown when the package cannot be acquired.</exception>
    public static IReadOnlyList<DiscoveredTemplate> DiscoverTemplates(string packageRoot)
    {
        var templates = new List<DiscoveredTemplate>();
        foreach (var manifestPath in Directory.EnumerateFiles(
                     packageRoot,
                     "template.json",
                     SearchOption.AllDirectories)
                     .Where(path => Path.GetFileName(Path.GetDirectoryName(path)) == ".template.config"))
        {
            var manifest = TemplateConfigParser.ParseFile(manifestPath);
            templates.Add(new DiscoveredTemplate(manifest, Path.GetDirectoryName(Path.GetDirectoryName(manifestPath))!));
        }

        if (templates.Count == 0)
        {
            throw new TemplatePackageAcquisitionError(
                $"no templates found under '{packageRoot}' — packages are scanned for .template.config/template.json at any depth.");
        }
        return templates;
    }

    /// <summary>
    /// Acquires a package from a feed into the store, or resolves an already-cached copy. Local
    /// folder feeds resolve directly from their .nupkg files.
    /// </summary>
    /// <param name="feed">The feed to download from.</param>
    /// <param name="packageId">The package id.</param>
    /// <param name="version">The exact version.</param>
    /// <returns>The directory the package is extracted into.</returns>
    /// <exception cref="TemplatePackageAcquisitionError">Thrown when the package cannot be acquired.</exception>
    public async Task<string> Acquire(NuGetFeed feed, string packageId, string version)
    {
        var packageRoot = Path.Combine(StoreRoot, packageId, version);
        var marker = Path.Combine(packageRoot, ".cratis-complete");
        if (File.Exists(marker))
        {
            return packageRoot;
        }

        if (Directory.Exists(packageRoot))
        {
            Directory.Delete(packageRoot, recursive: true);
        }

        if (NuGetClient.IsLocalFolder(feed))
        {
            await using var localPackage = NuGetClient.OpenLocalPackage(feed, packageId, version);
            NuGetClient.ExtractNupkg(localPackage, packageRoot);
        }
        else
        {
            await using var package = await _client.DownloadPackage(feed, packageId, version);
            NuGetClient.ExtractNupkg(package, packageRoot);
        }

        await File.WriteAllTextAsync(marker, string.Empty);
        return packageRoot;
    }

    /// <summary>
    /// Installs a local .nupkg file or an already-unpacked package directory into the store.
    /// </summary>
    /// <param name="path">Path to a .nupkg file or a package directory.</param>
    /// <returns>The directory the package is available under.</returns>
    /// <exception cref="TemplatePackageAcquisitionError">Thrown when the package cannot be acquired.</exception>
    public string InstallLocal(string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (Directory.Exists(fullPath))
        {
            return fullPath;
        }

        if (!File.Exists(fullPath) || !fullPath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
        {
            throw new TemplatePackageAcquisitionError($"'{path}' is neither a directory nor a .nupkg file.");
        }

        var packageId = Path.GetFileNameWithoutExtension(fullPath);
        var packageRoot = Path.Combine(StoreRoot, "local", packageId);
        if (Directory.Exists(packageRoot))
        {
            Directory.Delete(packageRoot, recursive: true);
        }
        using var archive = ZipFile.OpenRead(fullPath);
        NuGetClient.ExtractNupkg(new FileStream(fullPath, FileMode.Open, FileAccess.Read), packageRoot);
        return packageRoot;
    }

    /// <summary>
    /// Resolves the version of a package that satisfies the catalogue constraint, preferring cached versions
    /// so a cached package resolves offline. Local folder feeds resolve from their .nupkg file names.
    /// </summary>
    /// <param name="feed">The feed.</param>
    /// <param name="packageId">The package id.</param>
    /// <param name="versionPrefix">The version constraint — an exact version, a prefix, or * for latest.</param>
    /// <returns>The resolved version.</returns>
    /// <exception cref="TemplatePackageAcquisitionError">Thrown when the package cannot be acquired.</exception>
    public async Task<string> ResolveVersion(NuGetFeed feed, string packageId, string versionPrefix)
    {
        if (versionPrefix != "*")
        {
            return versionPrefix;
        }

        // Prefer an already-cached copy when offline resolution is needed.
        var cachedRoot = Path.Combine(StoreRoot, packageId);
        if (Directory.Exists(cachedRoot))
        {
            var cached = Directory.GetDirectories(cachedRoot)
                .Select(directory => Path.GetFileName(directory))
                .Where(name => Version.TryParse(name, out _))
                .OrderByDescending(Version.Parse)
                .ToArray();
            if (cached.Length > 0)
            {
                return cached[0];
            }
        }

        if (NuGetClient.IsLocalFolder(feed))
        {
            var localVersions = NuGetClient.GetLocalVersions(feed, packageId)
                ?? throw new TemplatePackageAcquisitionError(
                    $"package '{packageId}' was not found in local feed '{feed.Name}' ({feed.Url}).");
            return localVersions
                .Select(Version.Parse)
                .OrderDescending()
                .First()
                .ToString();
        }

        return await _client.GetLatestVersion(feed, packageId);
    }
}
