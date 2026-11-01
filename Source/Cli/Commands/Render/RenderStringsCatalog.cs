// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Represents the companion <c language="shell">.strings</c> files of a model and the locale that must contain every key.
/// </summary>
/// <param name="Files">The original contents of the files, keyed by their <c language="shell">/</c> separated path below the model root.</param>
/// <param name="DefaultLocale">The locale every referenced message key must exist in.</param>
internal sealed record RenderStringsCatalog(IReadOnlyDictionary<string, string> Files, string DefaultLocale)
{
    const string Extension = ".strings";
    const string PreferredLocale = "en";

    /// <summary>
    /// Reads the <c language="shell">&lt;base&gt;.&lt;locale&gt;.strings</c> files below a model root.
    /// </summary>
    /// <param name="root">The model root folder.</param>
    /// <returns>The catalog, or null when the model has no strings files.</returns>
    /// <remarks>
    /// The default locale is <c language="shell">en</c> when a file for it exists, otherwise the first locale in ordinal order.
    /// </remarks>
    public static RenderStringsCatalog? Read(string root)
    {
        if (!Directory.Exists(root))
        {
            return null;
        }

        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var locales = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(root, $"*{Extension}", SearchOption.AllDirectories))
        {
            var stem = Path.GetFileNameWithoutExtension(path);
            var separator = stem.LastIndexOf('.');
            if (separator <= 0)
            {
                continue;
            }

            files[Path.GetRelativePath(root, path).Replace('\\', '/')] = File.ReadAllText(path);
            locales.Add(stem[(separator + 1)..]);
        }

        return files.Count == 0
            ? null
            : new(files, locales.Contains(PreferredLocale) ? PreferredLocale : locales.First());
    }
}
