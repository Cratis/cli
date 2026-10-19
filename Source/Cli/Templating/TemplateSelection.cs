// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Templates;

/// <summary>
/// Where the template set <c language="bash">cratis new</c> uses came from.
/// </summary>
public enum TemplateSelectionOrigin
{
    /// <summary>The set recorded as installed under <c language="bash">~/.cratis/templates</c>.</summary>
    Installed = 0,

    /// <summary>Nothing was installed, so the latest set was downloaded and installed now.</summary>
    Downloaded = 1,

    /// <summary>Downloading failed, so a set already in the package store is used without being installed.</summary>
    Cached = 2,

    /// <summary>The version was given on the command line with <c language="bash">--version</c>.</summary>
    Requested = 3
}

/// <summary>
/// The template set <c language="bash">cratis new</c> uses.
/// </summary>
/// <param name="Version">The template version.</param>
/// <param name="Packages">The packages of the set.</param>
/// <param name="Origin">Where the set came from.</param>
/// <param name="Notice">Why a cached set is used instead of the latest one, if it is.</param>
public record TemplateSelection(string Version, IReadOnlyList<string> Packages, TemplateSelectionOrigin Origin, string? Notice = null);

/// <summary>
/// The outcome of checking for and installing a newer template set.
/// </summary>
/// <param name="PreviousVersion">The version installed before, if any.</param>
/// <param name="CurrentVersion">The version installed now, if any.</param>
/// <param name="Updated">Whether a different set was installed.</param>
/// <param name="Message">Why the latest set was not installed, if it was not.</param>
public record TemplateRefreshResult(string? PreviousVersion, string? CurrentVersion, bool Updated, string? Message = null);
