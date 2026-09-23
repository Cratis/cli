// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Llm;

namespace Cratis.Cli.Commands.Completions;

/// <summary>
/// Supplies bounded, offline completion suggestions. Model values are hints, not a closed vocabulary.
/// </summary>
internal static class OfflineCompletion
{
    const int MaxCandidates = 128;
    const int MaxOutputBytes = 8192;
    const int MaxCurrentLength = 1024;

    static readonly string[] _profiles =
    [
        "cratis/application/csharp",
        "cratis/application/typescript",
        "cratis/engineering/csharp",
        "cratis/engineering/typescript",
        "cratis/documentation",
        "cratis/screenplay",
        "cratis/stage",
        "cratis/modeling/screenplay-stage"
    ];
    static readonly string[] _harnesses = ["claude", "codex", "copilot", "cursor", "opencode", "pi"];
    static readonly string[] _languages = ["csharp", "typescript", "elixir", "java", "kotlin", "python"];
    static readonly string[] _catalogFiles = ["manifest.json", "profile-catalog.json"];
    static readonly string[] _catalogProfileGroups = ["publicProfiles", "engineeringProfiles"];
    static readonly string[] _staticContexts = ["output-formats", "contexts", "ai-profiles", "ai-harnesses", "ai-languages", "llm-kinds", "llm-models"];

    /// <summary>
    /// Determines whether the context can be completed without Chronicle.
    /// </summary>
    /// <param name="context">The completion context.</param>
    /// <returns>Whether the context is offline.</returns>
    public static bool IsStaticContext(string context) => _staticContexts.Contains(context, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns safe newline-delimited-shell candidates, retaining previous comma-separated selections.
    /// The list is capped at 128 items and 8192 UTF-8 bytes. No network or provider authentication is used.
    /// </summary>
    /// <param name="context">The completion context.</param>
    /// <param name="current">The word currently being completed.</param>
    /// <param name="project">The local project directory.</param>
    /// <returns>Safe suggestions, or an empty sequence when no local candidate matches.</returns>
    public static IReadOnlyList<string> Candidates(string context, string current, string project)
    {
        if (!IsStaticContext(context) || current.Length > MaxCurrentLength)
        {
            return [];
        }

        var key = context.ToLowerInvariant();
        var commaSeparated = key.StartsWith("ai-", StringComparison.Ordinal);
        var comma = commaSeparated ? current.LastIndexOf(',') : -1;
        var prefix = current[..(comma + 1)];
        var fragment = current[(comma + 1)..];
        if (current.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not ('-' or '_' or '.' or '/' or ',')) ||
            (!commaSeparated && comma >= 0))
        {
            return [];
        }

        var selected = commaSeparated ? current[..(comma + 1)].Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet(StringComparer.OrdinalIgnoreCase) : [];
        var values = key switch
        {
            "output-formats" => [OutputFormats.Table, OutputFormats.Plain, OutputFormats.Json, OutputFormats.JsonCompact, OutputFormats.Auto],
            "contexts" => CliConfiguration.Load().Contexts.Keys,
            "llm-kinds" => LlmKinds.All,
            "llm-models" => ModelHints(),
            "ai-profiles" => LocalSelection(project, "profiles", _profiles),
            "ai-harnesses" => LocalSelection(project, "harnesses", _harnesses),
            "ai-languages" => LocalSelection(project, "languages", _languages),
            _ => []
        };

        var result = new List<string>();
        var bytes = 0;
        foreach (var value in values.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Safe(value) || selected.Contains(value) || !value.StartsWith(fragment, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var candidate = prefix + value;
            var size = System.Text.Encoding.UTF8.GetByteCount(candidate) + 1;
            if (result.Count == MaxCandidates || bytes + size > MaxOutputBytes)
            {
                break;
            }

            result.Add(candidate);
            bytes += size;
        }

        return result;
    }

    static IEnumerable<string> ModelHints()
    {
        var configured = CliConfiguration.Load().Llm?.Model;
        if (configured is not null)
        {
            yield return configured;
        }

        foreach (var kind in LlmKinds.All)
        {
            if (LlmKinds.DefaultModelFor(kind) is { } model)
            {
                yield return model;
            }
        }
    }

    static string[] LocalSelection(string project, string dimension, string[] fallback)
    {
        var root = Path.Combine(project, ".cratis", "ai");
        foreach (var file in _catalogFiles)
        {
            var path = Path.Combine(root, file);
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                if (document.RootElement.TryGetProperty(dimension, out var array) && array.ValueKind == JsonValueKind.Array)
                {
                    return [.. array.EnumerateArray().Where(item => item.ValueKind == JsonValueKind.String)
                        .Select(item => item.GetString()!)];
                }

                if (dimension == "profiles")
                {
                    return [.. _catalogProfileGroups
                        .Where(name => document.RootElement.TryGetProperty(name, out var entries) && entries.ValueKind == JsonValueKind.Array)
                        .SelectMany(name => document.RootElement.GetProperty(name).EnumerateArray())
                        .Where(item => item.ValueKind == JsonValueKind.Object && item.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                        .Select(item => item.GetProperty("id").GetString()!)];
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
            {
                // An incomplete local catalog must not prevent the curated offline suggestions.
            }
        }

        return fallback;
    }

    static bool Safe(string value) => value.Length is > 0 and <= 128 && value.All(character =>
        char.IsAsciiLetterOrDigit(character) || character is '-' or '_' or '.' or '/');
}
