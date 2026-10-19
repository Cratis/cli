// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.ReleaseNotes;

/// <summary>
/// Settings for the <see cref="ReleaseNotesCommand"/>.
/// </summary>
public class ReleaseNotesSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the one release to show.
    /// </summary>
    [CommandOption("--version <VERSION>")]
    [Description("Show the notes of this release only")]
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the version to show what changed since, up to this build.
    /// </summary>
    [CommandOption("--since <VERSION>")]
    [Description("Show the releases after this version, up to this build - what 'cratis update' shows after updating")]
    public string? Since { get; set; }
}
