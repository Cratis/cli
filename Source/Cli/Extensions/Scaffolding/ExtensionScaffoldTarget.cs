// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.Scaffolding;

/// <summary>
/// Where the templates come from and where the package goes.
/// </summary>
/// <param name="Output">The folder to create the package in; null creates a folder named after the package in the current directory.</param>
/// <param name="TemplatePath">A local folder of templates to use instead of the template package.</param>
/// <param name="TemplatePackage">The template package to acquire; null uses <see cref="ExtensionTemplateInterface.PackageId"/>.</param>
/// <param name="TemplateVersion">The version constraint of the template package; null takes the newest release.</param>
/// <param name="DryRun">Whether to only report what would be created.</param>
/// <param name="Force">Whether to write into a folder that is not empty.</param>
public record ExtensionScaffoldTarget(
    string? Output = null,
    string? TemplatePath = null,
    string? TemplatePackage = null,
    string? TemplateVersion = null,
    bool DryRun = false,
    bool Force = false);
