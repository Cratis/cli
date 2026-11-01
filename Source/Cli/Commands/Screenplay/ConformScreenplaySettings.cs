// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Settings for comparing an authored model with application source.
/// </summary>
public class ConformScreenplaySettings : ScreenplaySourceSettings
{
    /// <summary>
    /// Gets or sets the authored root document or model folder.
    /// </summary>
    [CommandArgument(0, "<MODEL_ROOT>")]
    [Description("Authored .play root file (including imports), or folder containing one application.")]
    public string ModelRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the solution, project or folder to extract from.
    /// </summary>
    [CommandOption("--project <PATH>")]
    [Description("Solution, project or folder to read. Defaults to the current directory, searching upwards as generate does.")]
    public string? Project { get; set; }
}
