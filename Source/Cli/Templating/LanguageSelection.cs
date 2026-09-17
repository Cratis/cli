// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Templates;

/// <summary>
/// The result of resolving the <c language="csharp">--language</c> argument: the package carrying the language's
/// templates, the template to instantiate when none is named, and any errors.
/// </summary>
/// <param name="PackageId">The language's template package, or null when unpublished.</param>
/// <param name="DefaultTemplate">The template short name instantiated when none is named.</param>
/// <param name="Errors">Resolution errors, empty when the language resolves.</param>
public record LanguageSelectionResult(string? PackageId, string DefaultTemplate, IReadOnlyList<string> Errors);

/// <summary>
/// Resolves the <c language="csharp">--language</c> argument. Languages are matched case-insensitively; <c language="csharp">c#</c>
/// is accepted as an alias for <c language="csharp">csharp</c>. Each language maps to the package carrying its
/// templates and to the concept's default template name: the languages are derivatives of one
/// concept, so <c language="csharp">--language</c> selects the derivative inside the concept rather than a
/// separate template.
/// </summary>
public static class LanguageSelection
{
    /// <summary>
    /// Gets the supported languages, in their canonical lower-case forms.
    /// </summary>
    public static readonly string[] Supported =
    [
        "csharp", "kotlin", "java"
    ];

    /// <summary>
    /// Determines whether a language is supported, case-insensitively, accepting
    /// <c language="csharp">c#</c> as an alias for <c language="csharp">csharp</c>.
    /// </summary>
    /// <param name="language">The requested language.</param>
    /// <returns>True when the language is one of the supported values.</returns>
    public static bool IsSupported(string language) => Normalize(language) is not null;

    /// <summary>
    /// Normalizes a requested language to its canonical lower-case form, or null when the
    /// language is not supported.
    /// </summary>
    /// <param name="language">The requested language.</param>
    /// <returns>The canonical form, or null.</returns>
    public static string? Normalize(string language)
    {
        if (language.Equals("c#", StringComparison.OrdinalIgnoreCase))
        {
            return "csharp";
        }

        return Supported.FirstOrDefault(candidate =>
            candidate.Equals(language, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Resolves a normalized language against the catalogue.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when a supported language is missing from the catalogue.</exception>
    /// <param name="language">The canonical language form.</param>
    /// <returns>The package and default template, with errors when the language is unsupported or unpublished.</returns>
    public static LanguageSelectionResult Resolve(string language)
    {
        var normalized = Normalize(language);
        if (normalized is null)
        {
            return new LanguageSelectionResult(
                null,
                string.Empty,
                [$"--language '{language}' is not supported. Supported languages: {string.Join(", ", Supported)}."]);
        }

        var entry = TemplateCatalogue.FindLanguage(normalized)
            ?? throw new InvalidOperationException($"language '{normalized}' is supported but missing from the catalogue.");
        return new LanguageSelectionResult(entry.PackageId, entry.DefaultTemplate, []);
    }
}
