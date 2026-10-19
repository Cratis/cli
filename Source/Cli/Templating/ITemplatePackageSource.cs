// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Templates;

/// <summary>
/// Where the template installer gets template packages from: the configured NuGet feeds for new
/// versions and the local package store for versions already downloaded.
/// </summary>
public interface ITemplatePackageSource
{
    /// <summary>
    /// Asks the feeds for the latest stable version of a package.
    /// </summary>
    /// <param name="packageId">The package id.</param>
    /// <returns>The latest version.</returns>
    /// <exception cref="Cratis.Templating.TemplatePackageAcquisitionError">Thrown when no feed can answer.</exception>
    Task<string> GetLatestVersion(string packageId);

    /// <summary>
    /// Downloads a package version into the store, or returns the copy already there.
    /// </summary>
    /// <param name="packageId">The package id.</param>
    /// <param name="version">The exact version.</param>
    /// <returns>The directory the complete package is extracted into.</returns>
    /// <exception cref="Cratis.Templating.TemplatePackageAcquisitionError">Thrown when no feed can deliver it.</exception>
    Task<string> Acquire(string packageId, string version);

    /// <summary>
    /// Finds a package version that is already completely extracted in the store, without touching the network.
    /// </summary>
    /// <param name="packageId">The package id.</param>
    /// <param name="version">The exact version.</param>
    /// <returns>The package directory, or null when that version is not completely in the store.</returns>
    string? FindAcquired(string packageId, string version);

    /// <summary>
    /// Lists the versions of a package that are completely extracted in the store.
    /// </summary>
    /// <param name="packageId">The package id.</param>
    /// <returns>The versions, in no particular order.</returns>
    IReadOnlyList<string> AcquiredVersions(string packageId);
}
