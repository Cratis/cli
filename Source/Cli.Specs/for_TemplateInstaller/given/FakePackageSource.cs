// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;
using Cratis.Templating;

namespace Cratis.Cli.for_TemplateInstaller.given;

/// <summary>A package source over a scratch store; "publishing" makes a version downloadable without any network.</summary>
/// <param name="storeRoot">The scratch store root.</param>
public class FakePackageSource(string storeRoot) : ITemplatePackageSource
{
    readonly HashSet<(string PackageId, string Version)> _published = [];
    readonly Dictionary<(string PackageId, string Version), string> _catalogs = [];

    public bool Offline { get; set; }
    public HashSet<(string PackageId, string Version)> Failing { get; } = [];
    public int LatestVersionRequests { get; private set; }
    public List<(string PackageId, string Version)> Acquired { get; } = [];

    public void Publish(string version, string? catalog = null, params string[] packages)
    {
        foreach (var package in packages.Length > 0 ? packages : TemplateCatalogue.PinnedPackages)
        {
            _published.Add((package, version));
        }
        if (catalog is not null)
        {
            _catalogs[(TemplateCatalogue.DefaultPackageId, version)] = catalog;
        }
    }

    public void Cache(string version, params string[] packages)
    {
        foreach (var package in packages.Length > 0 ? packages : TemplateCatalogue.PinnedPackages)
        {
            Extract(package, version);
        }
    }

    public Task<string> GetLatestVersion(string packageId)
    {
        LatestVersionRequests++;
        ThrowWhenOffline();
        return Task.FromResult(TemplateVersions.Latest(_published.Where(entry => entry.PackageId == packageId).Select(entry => entry.Version))
            ?? throw new TemplatePackageAcquisitionError($"{packageId} is not published"));
    }

    public Task<string> Acquire(string packageId, string version)
    {
        if (FindAcquired(packageId, version) is { } existing)
        {
            return Task.FromResult(existing);
        }

        ThrowWhenOffline();
        if (!_published.Contains((packageId, version)) || Failing.Contains((packageId, version)))
        {
            throw new TemplatePackageAcquisitionError($"could not download {packageId} {version}");
        }

        Acquired.Add((packageId, version));
        return Task.FromResult(Extract(packageId, version));
    }

    public string? FindAcquired(string packageId, string version)
    {
        var root = Path.Combine(storeRoot, packageId, version);
        return File.Exists(Path.Combine(root, TemplatePackageSource.CompleteMarker)) ? root : null;
    }

    public IReadOnlyList<string> AcquiredVersions(string packageId)
    {
        var root = Path.Combine(storeRoot, packageId);
        return Directory.Exists(root)
            ? [.. Directory.GetDirectories(root).Where(directory => File.Exists(Path.Combine(directory, TemplatePackageSource.CompleteMarker))).Select(directory => Path.GetFileName(directory)).OfType<string>()]
            : [];
    }

    string Extract(string packageId, string version)
    {
        var root = Path.Combine(storeRoot, packageId, version);
        Directory.CreateDirectory(root);
        if (_catalogs.TryGetValue((packageId, version), out var catalog))
        {
            File.WriteAllText(Path.Combine(root, TemplateSetCatalog.FileName), catalog);
        }
        File.WriteAllText(Path.Combine(root, TemplatePackageSource.CompleteMarker), string.Empty);
        return root;
    }

    void ThrowWhenOffline()
    {
        if (Offline)
        {
            throw new TemplatePackageAcquisitionError("nuget.org: no such host is known");
        }
    }
}
