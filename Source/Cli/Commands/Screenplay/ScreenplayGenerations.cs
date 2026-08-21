// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Creates the <see cref="IScreenplayGeneration"/> this build of the CLI can offer.
/// </summary>
public static class ScreenplayGenerations
{
    /// <summary>
    /// Creates the generation to use.
    /// </summary>
    /// <returns>The <see cref="IScreenplayGeneration"/> to generate with.</returns>
    public static IScreenplayGeneration Create() => new ProviderScreenplayGeneration();
}
