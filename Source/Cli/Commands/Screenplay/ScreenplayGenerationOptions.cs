// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Represents the options that shape the generated Screenplay document.
/// </summary>
/// <param name="Domain">The domain the document belongs to; <see langword="null"/> lets the generator derive it from the compilation.</param>
/// <param name="Module">The module every discovered feature is placed within; <see langword="null"/> falls back to the domain.</param>
/// <param name="SegmentsToSkip">The number of leading namespace segments to skip when inferring features and slices; <see langword="null"/> uses the generator default.</param>
/// <param name="ModulesFromNamespaceRoots">Whether each feature is placed in a module named after the outermost segment of its namespace rather than all of them in one module.</param>
/// <param name="Provider">The source framework provider to use or auto-detect.</param>
public record ScreenplayGenerationOptions(
    string? Domain,
    string? Module,
    int? SegmentsToSkip,
    bool ModulesFromNamespaceRoots = false,
    string Provider = ScreenplayProviders.Auto)
{
    /// <summary>
    /// Gets the target framework to load from multi-targeted projects; <see langword="null"/> requires an explicit
    /// choice when a project targets several frameworks.
    /// </summary>
    public string? TargetFramework { get; init; }

    /// <summary>
    /// Gets the options that leave every choice to the generator.
    /// </summary>
    public static ScreenplayGenerationOptions Default { get; } = new(null, null, null, Provider: ScreenplayProviders.Auto);
}
