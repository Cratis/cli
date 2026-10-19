// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating;
using Cratis.Templating.Packages;

namespace Cratis.Cli.Templates;

/// <summary>
/// The template package source over the CLI's package store and the NuGet feeds configured for a
/// working directory; feeds are tried in order and the first one that answers wins.
/// </summary>
/// <param name="storeRoot">The package store root.</param>
/// <param name="workingDirectory">The directory whose NuGet configuration applies.</param>
public class TemplatePackageSource(string storeRoot, string workingDirectory) : ITemplatePackageSource
{
    /// <summary>
    /// The file the package store writes once a package is completely extracted.
    /// </summary>
    public const string CompleteMarker = ".cratis-complete";

    readonly NuGetClient _client = new();

    /// <inheritdoc/>
    public Task<string> GetLatestVersion(string packageId) =>
        FromFirstFeed(packageId, "the latest version of", feed => NuGetClient.IsLocalFolder(feed)
            ? Task.FromResult(TemplateVersions.Latest(NuGetClient.GetLocalVersions(feed, packageId) ?? [])
                ?? throw new TemplatePackageAcquisitionError($"package '{packageId}' was not found in local feed '{feed.Name}'."))
            : _client.GetLatestVersion(feed, packageId));

    /// <inheritdoc/>
    public Task<string> Acquire(string packageId, string version)
    {
        var store = new TemplatePackageStore(storeRoot, _client);
        return FromFirstFeed(packageId, $"version {version} of", feed => store.Acquire(feed, packageId, version));
    }

    /// <inheritdoc/>
    public string? FindAcquired(string packageId, string version)
    {
        var packageRoot = Path.Combine(storeRoot, packageId, version);
        return File.Exists(Path.Combine(packageRoot, CompleteMarker)) ? packageRoot : null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> AcquiredVersions(string packageId)
    {
        var packageRoot = Path.Combine(storeRoot, packageId);
        return Directory.Exists(packageRoot)
            ? [.. Directory.GetDirectories(packageRoot)
                .Where(directory => File.Exists(Path.Combine(directory, CompleteMarker)))
                .Select(directory => Path.GetFileName(directory))
                .OfType<string>()]
            : [];
    }

    async Task<string> FromFirstFeed(string packageId, string what, Func<NuGetFeed, Task<string>> fromFeed)
    {
        var failures = new List<string>();
        foreach (var feed in NuGetConfig.DiscoverFeeds(workingDirectory))
        {
            try
            {
                return await fromFeed(feed);
            }
            catch (Exception error) when (error is TemplatePackageAcquisitionError or HttpRequestException or TaskCanceledException or IOException)
            {
                failures.Add($"{feed.Name}: {error.Message}");
            }
        }

        throw new TemplatePackageAcquisitionError(
            $"could not get {what} '{packageId}' from any configured feed ({string.Join("; ", failures)}).");
    }
}
