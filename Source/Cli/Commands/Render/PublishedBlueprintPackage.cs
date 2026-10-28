// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Maps a published Scene domain blueprint, <c language="csharp">Cratis.Blueprint.&lt;Name&gt;</c>, to the npm package that publishes it.
/// </summary>
static class PublishedBlueprintPackage
{
    /// <summary>
    /// The documentation that explains how to approve a published blueprint.
    /// </summary>
    public const string DocumentationUrl = "https://github.com/Cratis/cli/blob/main/Documentation/reference/screenplay.md#use-a-published-scene-blueprint";

    const string Prefix = "Cratis.Blueprint.";
    const string Shipped = "Cratis.Blueprint.Default";

    /// <summary>
    /// Gets the npm package of a published domain blueprint.
    /// </summary>
    /// <param name="packageName">The runtime package name, for example <c language="csharp">Cratis.Blueprint.ProjectTracker</c>.</param>
    /// <returns>The npm package, for example <c language="csharp">@cratis/scene.blueprint.project-tracker</c>; null when the name is not a domain blueprint or is the one that ships with the renderer.</returns>
    public static string? NpmPackageFor(string? packageName)
    {
        if (packageName is null
            || packageName == Shipped
            || !packageName.StartsWith(Prefix, StringComparison.Ordinal)
            || !IsPascalName(packageName[Prefix.Length..]))
        {
            return null;
        }

        var kebab = new StringBuilder();
        foreach (var character in packageName[Prefix.Length..])
        {
            if (char.IsUpper(character) && kebab.Length > 0)
            {
                kebab.Append('-');
            }

            kebab.Append(char.ToLowerInvariant(character));
        }

        return $"@cratis/scene.blueprint.{kebab}";
    }

    /// <summary>
    /// Gets the sentence telling how to approve a published domain blueprint.
    /// </summary>
    /// <param name="packageName">The runtime package name.</param>
    /// <returns>The hint; null when the name is not a published domain blueprint.</returns>
    public static string? HintFor(string? packageName) =>
        NpmPackageFor(packageName) is { } npm
            ? $"{packageName} is published as the npm package {npm}; see {DocumentationUrl} for how to approve it."
            : null;

    /// <summary>
    /// Gets the blueprint a template-reference message says is not available.
    /// </summary>
    /// <param name="message">The finding message.</param>
    /// <returns>The blueprint name; null when the message is about something else.</returns>
    public static string? UnavailableBlueprintIn(string message)
    {
        const string start = "Blueprint '";
        const string end = " is not available";
        if (!message.StartsWith(start, StringComparison.Ordinal) || !message.EndsWith(end, StringComparison.Ordinal))
        {
            return null;
        }

        var close = message.IndexOf('\'', start.Length);
        return close > start.Length ? message[start.Length..close] : null;
    }

    static bool IsPascalName(string name) => name.Length > 0 && char.IsAsciiLetterUpper(name[0]) && name.All(char.IsAsciiLetterOrDigit);
}
