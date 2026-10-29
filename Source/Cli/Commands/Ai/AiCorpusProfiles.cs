// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Validates and expands the profiles a configuration selects from a corpus profile catalog.</summary>
static class AiCorpusProfiles
{
    static readonly Dictionary<string, string> _sections = new(StringComparer.Ordinal)
    {
        ["publicProfiles"] = "public",
        ["engineeringProfiles"] = "engineering"
    };

    internal static HashSet<string> Selected(string corpusPath, AiConfiguration configuration)
    {
        Validate(corpusPath, configuration);
        using var catalog = JsonDocument.Parse(File.ReadAllText(CatalogPath(corpusPath)));
        var selection = new Selection(CatalogProfiles(catalog), configuration.Languages);
        foreach (var profile in configuration.Profiles) selection.Select(profile, explicitSelection: true);
        return selection.Selected;
    }

    internal static IReadOnlyList<AiProfile> Listed(string corpusPath)
    {
        var path = CatalogPath(corpusPath);
        if (!File.Exists(path)) throw new InvalidOperationException("The Cratis AI corpus has no profile catalog (.cratis/ai/profile-catalog.json).");

        using var catalog = JsonDocument.Parse(File.ReadAllText(path));
        return [.. catalog.RootElement.EnumerateObject()
            .Where(property => _sections.ContainsKey(property.Name))
            .SelectMany(property => property.Value.EnumerateArray().Select(profile => new AiProfile(
                profile.GetProperty("id").GetString()!,
                _sections[property.Name],
                Strings(profile, "languages"),
                Strings(profile, "composes"))))];
    }

    internal static JsonElement[] CatalogProfiles(JsonDocument catalog) => [.. catalog.RootElement.EnumerateObject()
        .Where(property => _sections.ContainsKey(property.Name))
        .SelectMany(property => property.Value.EnumerateArray())];

    internal static IEnumerable<string> AvailableTargets(JsonElement profile) => profile.TryGetProperty("availableTargets", out var targets)
        ? targets.EnumerateArray().Select(target => target.GetString()!)
        : [];

    static string CatalogPath(string corpusPath) => Path.Combine(corpusPath, AiCorpusSynchronizer.ManagedRoot, "profile-catalog.json");

    static string[] Strings(JsonElement profile, string property) => profile.TryGetProperty(property, out var values)
        ? [.. values.EnumerateArray().Select(value => value.GetString()!)]
        : [];

    static void Validate(string corpusPath, AiConfiguration configuration)
    {
        var available = AiCorpusSynchronizer.Available(corpusPath);
        Validate("harness", configuration.Harnesses, available.Harnesses);
        Validate("profile", configuration.Profiles, available.Profiles);
        Validate("language", configuration.Languages, available.Languages);
    }

    static void Validate(string dimension, IEnumerable<string> selected, IEnumerable<string> available)
    {
        var known = available.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var value in selected)
        {
            if (!known.Contains(value)) throw new InvalidOperationException($"Cratis AI does not offer {dimension} '{value}'.");
        }
    }

    static bool SupportsAnyLanguage(JsonElement profile, IReadOnlyList<string> languages)
    {
        if (!profile.TryGetProperty("languages", out var supported)) return true;

        // A configuration that names no languages constrains nothing, so every language-specific profile
        // still composes. Treating the empty set as "matches nothing" would silently drop composed
        // profiles from a repository whose configuration simply omits the optional languages property.
        if (languages.Count == 0) return true;
        var values = supported.EnumerateArray().Select(language => language.GetString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return values.Contains("language-agnostic") || languages.Any(values.Contains);
    }

    sealed class Selection(IReadOnlyList<JsonElement> profiles, IReadOnlyList<string> languages)
    {
        internal HashSet<string> Selected { get; } = new(StringComparer.Ordinal);

        internal void Select(string id, bool explicitSelection)
        {
            var profile = profiles.FirstOrDefault(item => item.GetProperty("id").GetString() == id);
            if (profile.ValueKind == JsonValueKind.Undefined) throw new InvalidOperationException($"Unknown Cratis AI profile '{id}'.");
            if (!explicitSelection && !SupportsAnyLanguage(profile, languages)) return;
            if (!Selected.Add(id) || !profile.TryGetProperty("composes", out var composes)) return;
            foreach (var child in composes.EnumerateArray()) Select(child.GetString()!, explicitSelection: false);
        }
    }
}
