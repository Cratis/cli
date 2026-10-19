// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.Commands.New;

/// <summary>
/// Selects the template set <c language="bash">cratis new</c> works from: an explicit <c language="bash">--version</c>,
/// otherwise the set installed under <c language="bash">~/.cratis/templates</c> — downloading the latest on first use.
/// </summary>
public static class NewTemplateSelection
{
    /// <summary>
    /// Selects the template set for a run and tells the user when it was downloaded or a cached set is used.
    /// </summary>
    /// <param name="settings">The command settings.</param>
    /// <returns>The selected set.</returns>
    /// <exception cref="Cratis.Templating.TemplatePackageAcquisitionError">Thrown when no set is installed or cached and downloading fails.</exception>
    public static async Task<TemplateSelection> Select(NewSettings settings)
    {
        if (settings.Version is not null)
        {
            return new(settings.Version, TemplateCatalogue.PinnedPackages, TemplateSelectionOrigin.Requested);
        }

        // A package outside the Cratis set needs nothing installed: its latest version, or the newest cached one.
        if (settings.Package is not null && !TemplateCatalogue.PinnedPackages.Contains(settings.Package, StringComparer.Ordinal))
        {
            return new("*", [settings.Package], TemplateSelectionOrigin.Requested);
        }

        var selection = await TemplateInstaller.ForCurrentUser().EnsureInstalled();
        Report(settings, selection);
        return selection;
    }

    /// <summary>
    /// Gets the version to acquire a package at: the set's version for its own packages, the latest for any other.
    /// </summary>
    /// <param name="selection">The selected set.</param>
    /// <param name="packageId">The package to acquire.</param>
    /// <returns>The version constraint.</returns>
    public static string VersionFor(TemplateSelection selection, string packageId) =>
        selection.Origin == TemplateSelectionOrigin.Requested || selection.Packages.Contains(packageId, StringComparer.Ordinal)
            ? selection.Version
            : "*";

    static void Report(NewSettings settings, TemplateSelection selection)
    {
        if (settings.Format != "table" || selection.Origin == TemplateSelectionOrigin.Installed)
        {
            return;
        }

        var muted = OutputFormatter.Muted.ToMarkup();
        var message = selection.Origin == TemplateSelectionOrigin.Downloaded
            ? $"Installed Cratis templates {selection.Version} into ~/.cratis/templates."
            : $"Using cached Cratis templates {selection.Version}; the latest could not be installed: {selection.Notice}";
        AnsiConsole.MarkupLine($"  [{muted}]{message.EscapeMarkup()}[/]");
    }
}
