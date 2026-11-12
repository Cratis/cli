// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Scaffolding;

/// <summary>
/// The agreed interface between <c language="bash">cratis extension new</c> and the templates in Cratis/Templates: which package and
/// template the CLI instantiates for each kind, and which symbol values it passes. Documented at
/// https://github.com/Cratis/cli/blob/main/Documentation/reference/extension-new.md#the-template-interface.
/// </summary>
public static class ExtensionTemplateInterface
{
    /// <summary>The template package that carries both extension templates.</summary>
    public const string PackageId = "Cratis.Templates.Extensions";

    /// <summary>The short name of the template that creates a full extension (an OCI artifact).</summary>
    public const string ExtensionShortName = "cratis-extension";

    /// <summary>The short name of the template that creates a frontend-only component package (npm).</summary>
    public const string PackageShortName = "cratis-extension-package";

    /// <summary>The choice symbol that selects the backend language, or <c language="text">none</c>. Extension template only.</summary>
    public const string BackendSymbol = "Backend";

    /// <summary>The choice symbol that selects the frontend kind, or <c language="text">none</c>. Extension template only.</summary>
    public const string FrontendSymbol = "Frontend";

    /// <summary>The choice symbol that selects the component kind. Package template only.</summary>
    public const string ComponentSymbol = "Component";

    /// <summary>The text symbol holding the name a person sees.</summary>
    public const string DisplayNameSymbol = "DisplayName";

    /// <summary>The text symbol holding what the package does.</summary>
    public const string DescriptionSymbol = "Description";

    /// <summary>The text symbol holding the publisher (OCI namespace or npm scope).</summary>
    public const string PublisherSymbol = "Publisher";

    /// <summary>
    /// Gets the short name of the template for a kind of package.
    /// </summary>
    /// <param name="kind">The kind.</param>
    /// <returns>The short name.</returns>
    public static string ShortNameOf(ExtensionScaffoldKind kind) =>
        kind == ExtensionScaffoldKind.Extension ? ExtensionShortName : PackageShortName;

    /// <summary>
    /// Gets the symbol values the CLI passes to the template. The package name is passed as the template's name, not as a symbol.
    /// </summary>
    /// <param name="request">The validated request.</param>
    /// <returns>The symbol values by symbol name.</returns>
    public static IReadOnlyDictionary<string, string> ParametersOf(ExtensionScaffoldRequest request)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [DisplayNameSymbol] = request.DisplayName ?? ExtensionScaffoldValidator.SpellOut(request.Name),
            [DescriptionSymbol] = request.Description ?? $"The {ExtensionScaffoldValidator.SpellOut(request.Name)} extension",
            [PublisherSymbol] = request.Publisher ?? "example"
        };

        if (request.Kind == ExtensionScaffoldKind.Extension)
        {
            values[BackendSymbol] = request.Backend;
            values[FrontendSymbol] = request.Frontend;
        }
        else
        {
            values[ComponentSymbol] = request.Component;
        }

        return values;
    }
}
