// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Defines one bundled source-framework provider for Screenplay generation.
/// </summary>
interface IScreenplaySourceProvider
{
    /// <summary>
    /// Gets the stable provider name used by <c>--provider</c>.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets provider names this more-specific provider replaces when both match.
    /// </summary>
    IReadOnlyList<string> Supersedes { get; }

    /// <summary>
    /// Gets whether a solution with several deployable hosts is ambiguous for this provider.
    /// </summary>
    bool RequiresSingleHost { get; }

    /// <summary>
    /// Gets whether source evidence in the loaded compilations matches this provider.
    /// </summary>
    /// <param name="loaded">The loaded source compilations.</param>
    /// <returns><see langword="true"/> when the provider matches.</returns>
    bool Matches(LoadedCompilation loaded);

    /// <summary>
    /// Generates the Screenplay from already loaded source.
    /// </summary>
    /// <param name="loaded">The loaded source compilations.</param>
    /// <param name="targetPath">The source target path.</param>
    /// <param name="options">Generation options.</param>
    /// <returns>The generated Screenplay.</returns>
    GeneratedScreenplay GenerateFrom(
        LoadedCompilation loaded,
        string targetPath,
        ScreenplayGenerationOptions options);
}
