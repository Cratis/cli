// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Portions derived from dotnet/templating (https://github.com/dotnet/templating), licensed under the MIT license.
// Copyright (c) .NET Foundation and Contributors.

using Cratis.Templating.Configuration;

namespace Cratis.Templating;

/// <summary>
/// Parses <c language="csharp">template.json</c> documents into <see cref="TemplateConfig"/> instances. Parsing is strict:
/// unknown or malformed constructs throw named errors rather than being dropped.
/// </summary>
public static class TemplateConfigParser
{
    /// <summary>
    /// Parses a template manifest from disk, applying a localization overlay when one matches the current culture.
    /// </summary>
    /// <param name="path">Path to the template.json file.</param>
    /// <returns>The parsed <see cref="TemplateConfig"/>.</returns>
    public static TemplateConfig ParseFile(string path) =>
        LocalizationStore.ApplyFromDirectory(
            ParseDocument(File.ReadAllText(path)),
            Path.GetDirectoryName(path)!,
            System.Globalization.CultureInfo.CurrentCulture);

    /// <summary>
    /// Parses a template manifest JSON document.
    /// </summary>
    /// <param name="json">The manifest JSON.</param>
    /// <returns>The parsed <see cref="TemplateConfig"/>.</returns>
    public static TemplateConfig ParseDocument(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        return ParseElement(document.RootElement);
    }

    /// <summary>
    /// Parses a template manifest from a JSON element.
    /// </summary>
    /// <param name="root">The root object element.</param>
    /// <returns>The parsed <see cref="TemplateConfig"/>.</returns>
    /// <exception cref="InvalidTemplateManifest">Thrown when the manifest violates the template.json contract.</exception>
    public static TemplateConfig ParseElement(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidTemplateManifest("template.json must contain a JSON object at the root.");
        }

        Json.RejectUnknownProperties(root, "template.json", TemplateConfig.KnownTopLevelProperties);

        var name = Json.GetString(root, "name") ?? throw new InvalidTemplateManifest("template.json: 'name' is mandatory.");
        var shortName = Json.GetString(root, "shortName") ?? throw new InvalidTemplateManifest("template.json: 'shortName' is mandatory.");

        return new TemplateConfig
        {
            Name = name,
            ShortName = shortName,
            Author = Json.GetString(root, "author"),
            Identity = Json.GetString(root, "identity"),
            Description = Json.GetString(root, "description"),
            Classifications = Json.GetStringArray(root, "classifications"),
            Tags = ParseStringMap(root, "tags"),
            GroupIdentity = Json.GetString(root, "groupIdentity"),
            Precedence = Json.GetNumber(root, "precedence") ?? 0,
            DefaultName = Json.GetString(root, "defaultName"),
            PreferDefaultName = Json.GetBool(root, "preferDefaultName") ?? false,
            PreferNameDirectory = Json.GetBool(root, "preferNameDirectory") ?? false,
            PlaceholderFilename = Json.GetString(root, "placeholderFilename"),
            SourceName = Json.GetString(root, "sourceName"),
            Guids = ParseGuids(root),
            Symbols = SymbolParsing.ParseSymbols(root),
            Sources = ManifestSectionParsing.ParseSources(root),
            PostActions = ManifestSectionParsing.ParsePostActions(root),
            PrimaryOutputs = ManifestSectionParsing.ParsePrimaryOutputs(root),
            Constraints = ManifestSectionParsing.ParseConstraints(root),
            Baselines = ManifestSectionParsing.ParseBaselines(root),
            Forms = ManifestSectionParsing.ParseForms(root),
            GlobalCustomOperations = CustomOperationParsing.ParseOperations(
                Json.GetArray(root, "globalCustomOperations"), "globalCustomOperations", glob: null),
            SpecialCustomOperations = CustomOperationParsing.ParseSpecialOperations(root),
            ThirdPartyNotices = Json.GetString(root, "thirdPartyNotices"),
            GeneratorVersions = ParseGeneratorVersions(root)
        };
    }

    static Dictionary<string, string> ParseStringMap(JsonElement root, string property)
    {
        var result = new Dictionary<string, string>();
        foreach (var (key, value) in Json.GetObjectProperties(root, property))
        {
            result[key] = value.ValueKind == JsonValueKind.String
                ? value.GetString()!
                : throw new InvalidTemplateManifest($"template.json: '{property}.{key}' must be a string.");
        }
        return result;
    }

    static IReadOnlyList<string> ParseGuids(JsonElement root)
    {
        var guids = Json.GetStringArray(root, "guids");
        foreach (var guid in guids)
        {
            if (!Guid.TryParseExact(guid, "D", out _) && !Guid.TryParseExact(guid, "N", out _))
            {
                throw new InvalidTemplateManifest($"template.json: 'guids' entry '{guid}' is not a valid GUID.");
            }
        }
        return guids;
    }

    static Dictionary<string, string> ParseGeneratorVersions(JsonElement root)
    {
        if (!Json.TryGetProperty(root, "generatorVersions", out var value) || value.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        // The upstream corpus writes generator version constraints as a single string.
        if (value.ValueKind == JsonValueKind.String)
        {
            return new Dictionary<string, string> { ["*"] = value.GetString()! };
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidTemplateManifest("template.json: 'generatorVersions' must be an object or a version string.");
        }

        var result = new Dictionary<string, string>();
        foreach (var property in value.EnumerateObject())
        {
            result[property.Name] = property.Value.GetString() ?? string.Empty;
        }
        return result;
    }
}
