// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Render;

/// <summary>
/// Settings for the render command.
/// </summary>
public class RenderSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the document or folder to render.
    /// </summary>
    [CommandArgument(0, "[PATH]")]
    [Description("Screenplay (.play) file, or folder to render every .play file beneath. Defaults to the current directory.")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the directory to render into.
    /// </summary>
    /// <remarks>
    /// Named <c>--target</c> rather than <c>--output</c> because every command already carries a global
    /// <c>-o|--output</c> for the output <i>format</i>, and it is the renderer's own word for where it writes.
    /// </remarks>
    [CommandOption("--target <DIRECTORY>")]
    [Description("Directory to render the application into. Defaults to './out'.")]
    public string? Target { get; set; }
}
