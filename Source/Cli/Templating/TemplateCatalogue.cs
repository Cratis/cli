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
    /// Resolves a catalogue entry by short name, case-insensitively.
    /// </summary>
    /// <param name="shortName">The short name.</param>
    /// <returns>The entry, or null when not catalogued.</returns>
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
