// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Cratis.Templating.Packages;

/// <summary>
/// A minimal NuGet v3 client: resolves the flat-container resource from a feed's service index, lists
/// versions, downloads packages and resolves the latest stable version — no dependency on
/// <c language="csharp">NuGet.Protocol</c> and no <c language="csharp">dotnet</c> involvement.
/// </summary>
public class NuGetClient
{
    readonly HttpClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="NuGetClient"/> class.
    /// </summary>
    /// <param name="client">The HTTP client to use.</param>
    public NuGetClient(HttpClient? client = null)
    {
        _client = client ?? new HttpClient();
        _client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("cratis-cli", "1.0"));
    }

    /// <summary>
    /// Determines whether a feed URL points at a local folder rather than an HTTP service.
    /// </summary>
    /// <param name="feed">The feed.</param>
    /// <returns>True when the feed is a local folder.</returns>
    public static bool IsLocalFolder(NuGetFeed feed) =>
        !feed.Url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        && !feed.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Lists the package files for a package id available in a local folder feed.
    /// </summary>
    /// <param name="feed">The local folder feed.</param>
    /// <param name="packageId">The package id.</param>
    /// <returns>Available versions from the file names, or null when the feed does not carry the package.</returns>
    public static IReadOnlyList<string>? GetLocalVersions(NuGetFeed feed, string packageId)
    {
        var directory = new DirectoryInfo(feed.Url);
        if (!directory.Exists)
        {
            return null;
        }

        var prefix = $"{packageId}.";
        var versions = directory.GetFiles("*.nupkg")
            .Select(file => file.Name)
            .Where(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase))
            .Select(name => name[prefix.Length..^6])
            .Where(name => Version.TryParse(name, out _))
            .ToArray();
        return versions.Length > 0 ? versions : null;
    }

    /// <summary>
    /// Opens the nupkg file for a package in a local folder feed.
    /// </summary>
    /// <param name="feed">The local folder feed.</param>
    /// <param name="packageId">The package id.</param>
    /// <param name="version">The exact version.</param>
    /// <returns>A stream over the nupkg file.</returns>
    /// <exception cref="TemplatePackageAcquisitionError">Thrown when the package cannot be acquired.</exception>
    public static Stream OpenLocalPackage(NuGetFeed feed, string packageId, string version)
    {
        var path = Path.Combine(feed.Url, $"{packageId}.{version}.nupkg");
        if (!File.Exists(path))
        {
            throw new TemplatePackageAcquisitionError(
                $"package '{packageId}' {version} was not found in local feed '{feed.Name}' ({feed.Url}).");
        }
        return File.OpenRead(path);
    }

    /// <summary>
    /// Extracts a nupkg stream into a directory, ignoring package metadata entries.
    /// </summary>
    /// <param name="package">The nupkg stream.</param>
    /// <param name="targetDirectory">The directory to extract into.</param>
    /// <exception cref="TemplatePackageAcquisitionError">Thrown when the package cannot be acquired.</exception>
    public static void ExtractNupkg(Stream package, string targetDirectory)
    {
        using var archive = new ZipArchive(package, ZipArchiveMode.Read);
        Directory.CreateDirectory(targetDirectory);
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.EndsWith('/') || entry.FullName.StartsWith("_rels/") || entry.FullName.StartsWith("package/"))
            {
                continue;
            }

            var target = Path.GetFullPath(Path.Combine(targetDirectory, entry.FullName));
            if (!target.StartsWith(Path.GetFullPath(targetDirectory), StringComparison.Ordinal))
            {
                throw new TemplatePackageAcquisitionError($"package entry '{entry.FullName}' escapes the extraction directory.");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    /// <summary>
    /// Lists the available versions of a package from a feed, or null when the feed does not carry it.
    /// </summary>
    /// <param name="feed">The feed.</param>
    /// <param name="packageId">The package id.</param>
    /// <returns>The available versions, or null when not found.</returns>
    public async Task<IReadOnlyList<string>?> GetVersions(NuGetFeed feed, string packageId)
    {
        var baseAddress = await ResolveFlatContainerBase(feed);
        var response = await _client.GetAsync($"{baseAddress}/{packageId.ToLowerInvariant()}/index.json");
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.TryGetProperty("versions", out var versions)
            ? versions.EnumerateArray().Select(version => version.GetString()!).ToArray()
            : [];
    }

    /// <summary>
    /// Resolves the latest stable version of a package, preferring stable over prerelease.
    /// </summary>
    /// <param name="feed">The feed.</param>
    /// <param name="packageId">The package id.</param>
    /// <returns>The latest stable version, or the latest prerelease when only prereleases exist.</returns>
    /// <exception cref="TemplatePackageAcquisitionError">Thrown when the package cannot be acquired.</exception>
    public async Task<string> GetLatestVersion(NuGetFeed feed, string packageId)
    {
        var versions = await GetVersions(feed, packageId)
            ?? throw new TemplatePackageAcquisitionError($"package '{packageId}' was not found on feed '{feed.Name}' ({feed.Url}).");

        var stable = versions.Where(version => !version.Contains('-')).Select(Version.Parse).OrderDescending().ToArray();
        if (stable.Length > 0)
        {
            return stable[0].ToString();
        }

        // Only prereleases exist: pick the highest version part, then the highest prerelease label of it.
        var latest = versions
            .Select(version => (Full: version, Stable: Version.Parse(version.Split('-')[0])))
            .OrderByDescending(candidate => candidate.Stable)
            .ThenByDescending(candidate => candidate.Full, StringComparer.Ordinal)
            .First();
        return latest.Full;
    }

    /// <summary>
    /// Downloads a package as a stream.
    /// </summary>
    /// <param name="feed">The feed.</param>
    /// <param name="packageId">The package id.</param>
    /// <param name="version">The exact version.</param>
    /// <returns>A stream over the nupkg bytes.</returns>
    /// <exception cref="TemplatePackageAcquisitionError">Thrown when the package cannot be acquired.</exception>
    public async Task<Stream> DownloadPackage(NuGetFeed feed, string packageId, string version)
    {
        var baseAddress = await ResolveFlatContainerBase(feed);
        var id = packageId.ToLowerInvariant();
        var url = $"{baseAddress}/{id}/{version.ToLowerInvariant()}/{id}.{version.ToLowerInvariant()}.nupkg";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (feed.Username is not null && feed.Password is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{feed.Username}:{feed.Password}")));
        }

        var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new TemplatePackageAcquisitionError(
                $"failed to download '{packageId}' {version} from feed '{feed.Name}': {(int)response.StatusCode} {response.ReasonPhrase}.");
        }
        return await response.Content.ReadAsStreamAsync();
    }

    async Task<string> ResolveFlatContainerBase(NuGetFeed feed)
    {
        var response = await _client.GetAsync(feed.Url);
        if (!response.IsSuccessStatusCode)
        {
            throw new TemplatePackageAcquisitionError(
                $"failed to read service index for feed '{feed.Name}' ({feed.Url}): {(int)response.StatusCode} {response.ReasonPhrase}.");
        }

        var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        foreach (var resource in document.RootElement.GetProperty("resources").EnumerateArray())
        {
            if (!resource.TryGetProperty("@type", out var type))
            {
                continue;
            }

            // The flat container is typed "PackageBaseAddress/3.0.0" on nuget.org and
            // "FlatContainer" on several proxy feeds — accept both.
            var resourceType = type.GetString();
            if (resourceType?.StartsWith("PackageBaseAddress", StringComparison.Ordinal) != true
                && resourceType?.StartsWith("FlatContainer", StringComparison.Ordinal) != true)
            {
                continue;
            }

            if (resource.TryGetProperty("@id", out var id))
            {
                return id.GetString()!.TrimEnd('/');
            }
        }

        throw new TemplatePackageAcquisitionError($"feed '{feed.Name}' ({feed.Url}) does not expose a flat container resource.");
    }
}
