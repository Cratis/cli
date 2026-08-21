// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Defines the source-framework providers available for Screenplay generation.
/// </summary>
public static class ScreenplayProviders
{
    /// <summary>
    /// Detect the provider from loaded project compilations.
    /// </summary>
    public const string Auto = "auto";

    /// <summary>
    /// Generate from Arc and Chronicle conventions.
    /// </summary>
    public const string Arc = "arc";

    /// <summary>
    /// Generate from Marten conventions without requiring Wolverine.
    /// </summary>
    public const string Marten = "marten";

    /// <summary>
    /// Generate from combined Marten and Wolverine conventions.
    /// </summary>
    public const string CritterStack = "critter-stack";

    static readonly HashSet<string> _known = [Auto, Arc, Marten, CritterStack];

    /// <summary>
    /// Gets whether a provider name is recognized.
    /// </summary>
    /// <param name="provider">The provider name.</param>
    /// <returns><see langword="true"/> when recognized; otherwise, <see langword="false"/>.</returns>
    public static bool IsKnown(string provider) => _known.Contains(provider);
}
