// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Version;
using Cratis.Templating;

namespace Cratis.Cli.Templates;

/// <summary>
/// Installs the Cratis template set under <c language="bash">~/.cratis/templates</c>. The first <c language="bash">cratis new</c>
/// without an installed set downloads the latest one from NuGet; <c language="bash">cratis update</c> checks for a
/// newer one. A set is recorded as installed only after every one of its packages is completely in the
/// store, so a failed or partial download never replaces a working installation.
/// </summary>
/// <param name="source">Where packages come from.</param>
/// <param name="templatesRoot">The templates root, normally <c language="bash">~/.cratis/templates</c>.</param>
/// <param name="cliVersion">The version of the running CLI.</param>
public class TemplateInstaller(ITemplatePackageSource source, string templatesRoot, string cliVersion)
{
    const string RootPackage = TemplateCatalogue.DefaultPackageId;

    /// <summary>
    /// Creates the installer for the current user's template store and the NuGet feeds of the current directory.
    /// </summary>
    /// <returns>The installer.</returns>
    public static TemplateInstaller ForCurrentUser() => new(
        new TemplatePackageSource(TemplateCatalogue.StoreRoot(), Environment.CurrentDirectory),
        TemplateCatalogue.TemplatesRoot(),
        VersionCommand.GetCliVersion());

    /// <summary>
    /// Gets the template set to use, downloading the latest one when none is installed. Once a set is
    /// installed this never touches the network.
    /// </summary>
    /// <returns>The selected set.</returns>
    /// <exception cref="TemplatePackageAcquisitionError">Thrown when nothing is installed or cached and downloading fails.</exception>
    public async Task<TemplateSelection> EnsureInstalled()
    {
        var installed = InstalledTemplates.Read(templatesRoot);
        if (installed is not null && IsComplete(installed.Version, installed.Packages))
        {
            return new(installed.Version, installed.Packages, TemplateSelectionOrigin.Installed);
        }

        string failure;
        try
        {
            var (downloaded, reason) = await Install(await source.GetLatestVersion(RootPackage), cliVersion);
            if (downloaded is not null)
            {
                return new(downloaded.Version, downloaded.Packages, TemplateSelectionOrigin.Downloaded);
            }
            failure = reason!;
        }
        catch (TemplatePackageAcquisitionError error)
        {
            failure = error.Message;
        }

        return NewestCachedSet() is { } cached
            ? cached with { Notice = failure }
            : throw new TemplatePackageAcquisitionError(
                $"no Cratis templates are installed in '{templatesRoot}' and the latest could not be downloaded: {failure} " +
                "Run 'cratis new' or 'cratis update' again once NuGet is reachable, or pass --template-path with a local package.");
    }

    /// <summary>
    /// Checks the feeds for a newer template set and installs it. The installed set stays in place when the
    /// check or a download fails, when the newer set needs a newer CLI, or when the installed set is newer.
    /// </summary>
    /// <param name="effectiveCliVersion">The CLI version the set must work with — the newly installed one after a CLI update.</param>
    /// <returns>The outcome.</returns>
    public async Task<TemplateRefreshResult> Refresh(string? effectiveCliVersion = null)
    {
        var previous = InstalledTemplates.Read(templatesRoot);
        try
        {
            var latest = await source.GetLatestVersion(RootPackage);
            if (previous is not null && IsComplete(previous.Version, previous.Packages)
                && TemplateVersions.Parse(previous.Version) >= TemplateVersions.Parse(latest))
            {
                return new(previous.Version, previous.Version, false);
            }

            var (installed, reason) = await Install(latest, effectiveCliVersion ?? cliVersion);
            return installed is null
                ? new(previous?.Version, previous?.Version, false, reason)
                : new(previous?.Version, installed.Version, previous?.Version != installed.Version);
        }
        catch (TemplatePackageAcquisitionError error)
        {
            return new(previous?.Version, previous?.Version, false, error.Message);
        }
    }

    async Task<(InstalledTemplates? Installed, string? Reason)> Install(string version, string forCliVersion)
    {
        var catalog = TemplateSetCatalog.ReadFrom(await source.Acquire(RootPackage, version));
        if (!TemplateVersions.IsSatisfiedBy(forCliVersion, catalog.MinimumCliVersion))
        {
            return (null, $"templates {version} need Cratis CLI {catalog.MinimumCliVersion} or later (this is {forCliVersion}); run 'cratis update'.");
        }

        foreach (var package in catalog.Packages.Where(package => package != RootPackage))
        {
            await source.Acquire(package, version);
        }

        var installed = new InstalledTemplates(version, catalog.Packages, catalog.MinimumCliVersion, DateTimeOffset.UtcNow);
        installed.Write(templatesRoot);
        return (installed, null);
    }

    bool IsComplete(string version, IEnumerable<string> packages) =>
        packages.All(package => source.FindAcquired(package, version) is not null);

    TemplateSelection? NewestCachedSet()
    {
        var versions = source.AcquiredVersions(RootPackage)
            .Where(version => TemplateVersions.Parse(version) is not null)
            .OrderByDescending(TemplateVersions.Parse);
        foreach (var version in versions)
        {
            var catalog = TemplateSetCatalog.ReadFrom(source.FindAcquired(RootPackage, version)!);
            if (TemplateVersions.IsSatisfiedBy(cliVersion, catalog.MinimumCliVersion) && IsComplete(version, catalog.Packages))
            {
                return new(version, catalog.Packages, TemplateSelectionOrigin.Cached);
            }
        }
        return null;
    }
}
