// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Ai;

/// <summary>
/// Options for listing profiles offered by the Cratis AI corpus.
/// </summary>
public sealed class AiProfilesListSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets a local checkout of Cratis/AI.
    /// </summary>
    [CommandOption("--source <PATH>")]
    [Description("Read the corpus from a local Cratis/AI checkout instead of downloading it. Pins a revision and works offline. Env: CRATIS_AI_SOURCE")]
    public string? Source { get; set; }

    /// <summary>
    /// Gets or sets whether every profile is listed, rather than only the ones the corpus recommends.
    /// </summary>
    [CommandOption("--all")]
    [Description("List every profile, not only the recommended starting points. JSON output always lists every profile")]
    public bool All { get; set; }
}
