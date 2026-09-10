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
    [Description("Screenplay (.play) file, or folder representing one logical application. Defaults to the current directory.")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the statically bundled renderer target.
    /// </summary>
    [CommandOption("--target <TARGET>")]
    [Description("Renderer target. The initial bundled target is 'cratis'.")]
    public string? Target { get; set; }

    /// <summary>
    /// Gets or sets the directory to publish into.
    /// </summary>
    [CommandOption("--destination <DIRECTORY>")]
    [Description("Directory to publish managed artifacts into. Defaults to './out'.")]
    public string? Destination { get; set; }

    /// <summary>
    /// Gets or sets the destination-independent application identity and root namespace.
    /// </summary>
    [CommandOption("--name <NAME>")]
    [Description("Application name and root namespace. Required and independent of the destination path.")]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether modified active managed artifacts may be replaced.
    /// </summary>
    [CommandOption("--force")]
    [Description("Replace modified active managed artifacts; never overwrite unmanaged files or remove modified stale files.")]
    public bool Force { get; set; }
}
