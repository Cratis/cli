// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using System.Globalization;
using Cratis.Templating.Configuration;

namespace Cratis.Templating;

/// <summary>
/// Applies localization overlays (<c language="csharp">localize/templatestrings.*.json</c>) to parsed manifests, keyed by
/// dot paths into the manifest such as <c language="csharp">symbols/Framework/description</c>. Resolution follows the current
/// culture: exact culture, then parent culture, then the manifest itself as the invariant fallback.
/// </summary>
public static class LocalizationStore
{
    /// <summary>
    /// Loads the best matching localization file next to a manifest and applies it.
    /// </summary>
    /// <param name="manifest">The manifest to overlay.</param>
    /// <param name="manifestDirectory">Directory containing the manifest (its localize subfolder is used).</param>
    /// <returns>A manifest with localized strings applied.</returns>
    public static TemplateConfig ApplyFromDirectory(TemplateConfig manifest, string manifestDirectory)
    {
        var localizeDirectory = Path.Combine(manifestDirectory, "localize");
        if (!Directory.Exists(localizeDirectory))
        {
            return manifest;
        }

        var culture = CultureInfo.CurrentCulture;
        foreach (var candidate in new[] { culture.Name, culture.TwoLetterISOLanguageName })
        {
            if (string.IsNullOrEmpty(candidate))
            {
                continue;
            }

            var path = Path.Combine(localizeDirectory, $"templatestrings.{candidate}.json");
            if (File.Exists(path))
            {
                return ApplyFromFile(manifest, path);
            }
        }
        return manifest;
    }

    /// <summary>
    /// Applies a specific localization file to a manifest.
    /// </summary>
    /// <param name="manifest">The manifest to overlay.</param>
    /// <param name="path">Path to the templatestrings file.</param>
    /// <returns>A manifest with localized strings applied.</returns>
    public static TemplateConfig ApplyFromFile(TemplateConfig manifest, string path)
    {
        using var document = JsonDocument.Parse(
            File.ReadAllText(path),
            new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        var strings = document.RootElement.EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetString() ?? string.Empty);
        return Apply(manifest, strings);
    }

    /// <summary>
    /// Applies a string table to a manifest.
    /// </summary>
    /// <param name="manifest">The manifest to overlay.</param>
    /// <param name="strings">Localized strings keyed by manifest path.</param>
    /// <returns>A manifest with localized strings applied.</returns>
    public static TemplateConfig Apply(TemplateConfig manifest, IReadOnlyDictionary<string, string> strings)
    {
        if (strings.Count == 0)
        {
            return manifest;
        }

        var symbols = manifest.Symbols.ToDictionary(
            pair => pair.Key,
            pair => LocalizeSymbol(pair.Value, strings));
        var postActions = manifest.PostActions
            .Select((action, index) => action with
            {
                Description = Localize($"postActions/{index}/description", action.Description, strings),
                ManualInstructions = [.. action.ManualInstructions
                    .Select((instruction, instructionIndex) => instruction with
                    {
                        Text = Localize($"postActions/{index}/manualInstructions/{instructionIndex}/text", instruction.Text, strings) ?? string.Empty
                    })]
            })
            .ToArray();
        var baselines = manifest.Baselines.ToDictionary(
            pair => pair.Key,
            pair => pair.Value with
            {
                Description = Localize($"baselines/{pair.Key}/description", pair.Value.Description, strings)
            });

        return manifest with
        {
            Author = Localize("author", manifest.Author, strings),
            Name = Localize("name", manifest.Name, strings)!,
            Description = Localize("description", manifest.Description, strings),
            Symbols = symbols,
            PostActions = postActions,
            Baselines = baselines
        };
    }

    static SymbolConfig LocalizeSymbol(SymbolConfig symbol, IReadOnlyDictionary<string, string> strings) => symbol with
    {
        Description = Localize($"symbols/{symbol.Name}/description", symbol.Description, strings),
        DisplayName = Localize($"symbols/{symbol.Name}/displayName", symbol.DisplayName, strings),
        Choices = [.. symbol.Choices
            .Select(choice => choice with
            {
                Description = Localize($"symbols/{symbol.Name}/choices/{choice.Choice}/description", choice.Description, strings)
            })]
    };

    static string? Localize(string key, string? fallback, IReadOnlyDictionary<string, string> strings) =>
        strings.TryGetValue(key, out var value) ? value : fallback;
}
