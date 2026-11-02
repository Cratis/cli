// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.Commands.Ai;

/// <summary>Classifies the languages a rule targets from the file extensions in its applyTo globs.</summary>
static class AiRuleLanguages
{
    static readonly Dictionary<string, string> _languageByExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["cs"] = "csharp",
        ["csproj"] = "csharp",
        ["csx"] = "csharp",
        ["ts"] = "typescript",
        ["tsx"] = "typescript",
        ["mts"] = "typescript",
        ["cts"] = "typescript",
        ["ex"] = "elixir",
        ["exs"] = "elixir",
        ["py"] = "python",
        ["go"] = "go",
        ["kt"] = "kotlin",
        ["kts"] = "kotlin",
        ["java"] = "java",
    };

    /// <summary>Gets the recognized languages whose file extensions appear in the globs.</summary>
    /// <param name="applyTo">The applyTo frontmatter value.</param>
    /// <returns>The distinct recognized language identifiers.</returns>
    internal static IReadOnlySet<string> From(string applyTo) =>
        Extensions(applyTo)
            .Where(_languageByExtension.ContainsKey)
            .Select(extension => _languageByExtension[extension])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    static IEnumerable<string> Extensions(string applyTo) =>
        Globs(applyTo).SelectMany(GlobExtensions);

    static IEnumerable<string> GlobExtensions(string glob)
    {
        var dot = glob.LastIndexOf('.');
        if (dot < 0 || dot == glob.Length - 1) return [];

        // "*.{cs,ts}" lists several extensions; otherwise the extension follows the last dot.
        return glob[(dot + 1)..].Trim('{', '}').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    static IEnumerable<string> Globs(string applyTo)
    {
        var depth = 0;
        var current = new StringBuilder();
        foreach (var character in applyTo)
        {
            depth += BraceDelta(character);
            if (character == ',' && depth == 0)
            {
                yield return current.ToString();
                current.Clear();
            }
            else
            {
                current.Append(character);
            }
        }

        yield return current.ToString();
    }

    static int BraceDelta(char character) => character switch
    {
        '{' => 1,
        '}' => -1,
        _ => 0
    };
}
