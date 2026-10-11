// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Screenplay;
using Cratis.Screenplay.Syntax;

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Refuses navigation contribution properties the bundled Stage Scene converter cannot preserve.
/// </summary>
internal static class NavigationContributionAdmission
{
    /// <summary>
    /// The diagnostic code for a navigation contribution property not supported by the bundled renderer.
    /// </summary>
    public const string UnsupportedPropertyCode = "CLI-RENDER-015";

    /// <summary>
    /// Checks contributions throughout the authored module and feature hierarchy.
    /// </summary>
    /// <param name="application">The parsed application before Scene conversion.</param>
    /// <returns>Blocking diagnostics naming each unsupported contribution property.</returns>
    public static IEnumerable<ScreenplayDiagnostic> Check(ApplicationSyntax application) => application.Modules.SelectMany(module =>
        Check(module.Contributions ?? [], module.Name).Concat(CheckFeatures(module.Features, module.Name)));

    static IEnumerable<ScreenplayDiagnostic> CheckFeatures(IEnumerable<FeatureSyntax> features, string parent) => features.SelectMany(feature =>
        Check(feature.Contributions ?? [], $"{parent}.{feature.Name}").Concat(CheckFeatures(feature.Features, $"{parent}.{feature.Name}")));

    static IEnumerable<ScreenplayDiagnostic> Check(IEnumerable<ContributionSyntax> contributions, string parent) => contributions.SelectMany((contribution, index) =>
    {
        var name = $"{parent}.contribute to {contribution.ContributionPoint}[{index}]";
        (string Property, object? Value)[] properties =
        [
            ("id", contribution.Id),
            ("icon", contribution.Icon),
            ("presentation", contribution.Presentation),
            ("group", contribution.Group),
            ("destination", contribution.Destination)
        ];

        return properties.Where(property => property.Value is not null).Select(property => new ScreenplayDiagnostic(
            ScreenplayDiagnosticSeverity.Error,
            UnsupportedPropertyCode,
            $"Navigation contribution '{name}' property '{property.Property}' is not supported by the bundled Stage renderer yet.",
            $"{contribution.Location.Path ?? "Screenplay"}({contribution.Location.Line},{contribution.Location.Column})"));
    });
}
