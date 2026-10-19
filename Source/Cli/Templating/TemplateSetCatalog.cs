// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Templates;

/// <summary>
/// The catalog a template set carries: the packages that make up the set and the minimum CLI version
/// that can use it. Authored in the Cratis/Templates repository and shipped as <c language="bash">catalog.json</c>
/// at the root of the <c language="bash">Cratis.Templates</c> package; a package without one is described by the
/// catalogue built into the CLI.
/// </summary>
/// <param name="Packages">The packages of the set, released together at the same version.</param>
/// <param name="MinimumCliVersion">The minimum CLI version, or null when any CLI can use it.</param>
public record TemplateSetCatalog(IReadOnlyList<string> Packages, string? MinimumCliVersion)
{
    /// <summary>
    /// The catalog's file name at the root of the package.
    /// </summary>
    public const string FileName = "catalog.json";

    /// <summary>
    /// Gets the catalog the CLI uses when a package carries none.
    /// </summary>
    public static TemplateSetCatalog BuiltIn { get; } = new(TemplateCatalogue.PinnedPackages, null);

    /// <summary>
    /// Reads the catalog from an extracted package.
    /// </summary>
    /// <param name="packageRoot">The extracted root package.</param>
    /// <returns>The package's catalog, or <see cref="BuiltIn"/> when it carries none or it cannot be read.</returns>
    public static TemplateSetCatalog ReadFrom(string packageRoot)
    {
        var path = Path.Combine(packageRoot, FileName);
        if (!File.Exists(path))
        {
            return BuiltIn;
        }

        try
        {
            var document = JsonNode.Parse(File.ReadAllText(path))?.AsObject();
            var packages = document?["packages"]?.AsArray().Select(package => package!.GetValue<string>()).ToArray() ?? [];
            var minimum = document?["minimumCliVersion"]?.GetValue<string>();
            var withRoot = packages.Contains(TemplateCatalogue.DefaultPackageId, StringComparer.Ordinal)
                ? packages
                : [TemplateCatalogue.DefaultPackageId, .. packages];
            return new(withRoot, minimum);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return BuiltIn;
        }
    }
}
