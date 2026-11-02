// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>Classifies what a rule targets from the file patterns in its applyTo globs.</summary>
static class AiRuleLanguages
{
    const int MaximumAlternatives = 256;

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

    static readonly HashSet<string> _documentationExtensions = new(StringComparer.OrdinalIgnoreCase) { "md", "mdx" };

    /// <summary>Classifies every concrete pattern the applyTo globs describe.</summary>
    /// <param name="applyTo">The applyTo frontmatter value.</param>
    /// <returns>The recognized languages, whether documentation is targeted, and whether any pattern is language independent.</returns>
    internal static AiRuleApplicability Classify(string applyTo)
    {
        var patterns = AiGlobAlternatives.Expand(applyTo, MaximumAlternatives);
        if (patterns is null) return new(new HashSet<string>(StringComparer.OrdinalIgnoreCase), false, true);

        var languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var documentation = false;
        var universal = false;
        foreach (var extension in patterns.Select(Extension))
        {
            if (extension is not null && _languageByExtension.TryGetValue(extension, out var language)) languages.Add(language);
            else if (extension is not null && _documentationExtensions.Contains(extension)) documentation = true;
            else universal = true;
        }

        return new(languages, documentation, universal);
    }

    static string? Extension(string pattern)
    {
        var name = pattern[(pattern.LastIndexOf('/') + 1)..];
        var dot = name.LastIndexOf('.');
        return dot < 0 || dot == name.Length - 1 ? null : name[(dot + 1)..];
    }
}
