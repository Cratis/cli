// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Packages;

namespace Cratis.Cli.Templates;

/// <summary>
/// One template offered by the CLI's programmatic catalogue.
/// </summary>
/// <param name="ShortName">The short name used for instantiation.</param>
/// <param name="Name">The template's display name.</param>
/// <param name="Description">The template description.</param>
/// <param name="Identity">The template identity inside the package.</param>
/// <param name="PackageId">The package that carries the template.</param>
/// <param name="Version">The package version the CLI pins for this release.</param>
public record CataloguedTemplate(
    string ShortName,
    string Name,
    string Description,
    string Identity,
    string PackageId,
    string Version)
{
    /// <summary>
    /// Gets the template's documentation page slug.
    /// </summary>
    public string DocsSlug => ShortName;
}

/// <summary>
/// One supported language in the catalogue: its default template short name and the package
/// that carries it, or null when that language's template package is not published yet.
/// </summary>
/// <param name="Language">The language name, in its canonical lower-case form.</param>
/// <param name="DefaultTemplate">The template instantiated when no positional template is given.</param>
/// <param name="PackageId">The package carrying the language's templates, or null when unpublished.</param>
public record CataloguedLanguage(string Language, string DefaultTemplate, string? PackageId)
{
    /// <summary>
    /// Gets a value indicating whether the language's template package is published.
    /// </summary>
    public bool IsPublished => PackageId is not null;
}

/// <summary>
/// The v1 programmatic template catalogue: the CLI knows which template packages it offers and at which
/// pinned version. The design does not preclude user-managed install/uninstall later — every entry
/// resolves through the same package store and discovery scan.
/// </summary>
public static class TemplateCatalogue
{
    /// <summary>
    /// Gets the package the catalogue ships with, pinned per CLI release; <c language="csharp">--version</c> overrides it.
    /// </summary>
    public const string DefaultPackageId = "Cratis.Templates";

    /// <summary>
    /// Gets the pinned version of the catalogue package for this CLI release.
    /// </summary>
    public const string DefaultVersion = "1.3.0";

    /// <summary>
    /// Gets the language table: each supported language's default template short name and the
    /// package that carries it. Kotlin and Java ship from their own packages — their entries
    /// carry null until those packages are published.
    /// </summary>
    public static readonly IReadOnlyList<CataloguedLanguage> Languages =
    [
        new("csharp", "cratis", DefaultPackageId),
        new("kotlin", "cratis-kotlin", null),
        new("java", "cratis-java", null)
    ];

    /// <summary>
    /// Lists the templates the CLI offers from the default package.
    /// </summary>
    /// <returns>The catalogued templates.</returns>
    public static IReadOnlyList<CataloguedTemplate> List() =>
    [
        new("cratis", "Cratis Web Application", "A template for creating a Cratis web application", "Cratis.Templates.Web", DefaultPackageId, DefaultVersion),
        new("cratis-aspire", "Cratis Aspire Application", "A template for creating a Cratis web application with .NET Aspire orchestration", "Cratis.Templates.Aspire", DefaultPackageId, DefaultVersion),
        new("cratis-chronicle-console", "Cratis Chronicle Console", "A console app template that connects to Cratis Chronicle", "Cratis.Templates.ChronicleConsole", DefaultPackageId, DefaultVersion),
        new("cratis-chronicle-web", "Cratis Chronicle Web", "A simple web app template that connects to Cratis Chronicle", "Cratis.Templates.ChronicleWeb", DefaultPackageId, DefaultVersion)
    ];

    /// <summary>
    /// Resolves a language entry from the language table, case-insensitively.
    /// </summary>
    /// <param name="language">The language name.</param>
    /// <returns>The entry, or null when the language is not catalogued.</returns>
    public static CataloguedLanguage? FindLanguage(string language) =>
        Languages.FirstOrDefault(entry => entry.Language.Equals(language, StringComparison.OrdinalIgnoreCase));

    public static CataloguedTemplate? Find(string shortName) =>
        List().FirstOrDefault(template => template.ShortName.Equals(shortName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Gets the CLI's template store root, isolated from the dotnet template store.
    /// </summary>
    /// <returns>The absolute store path.</returns>
    public static string StoreRoot() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        ".cratis",
        "templates",
        "packages");

    /// <summary>
    /// Creates the templating engine over the CLI's store.
    /// </summary>
    /// <returns>The engine.</returns>
    public static Templating.TemplatingEngine CreateEngine() => new(StoreRoot());
}
