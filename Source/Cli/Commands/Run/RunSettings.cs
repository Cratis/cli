// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Settings for the run command.
/// </summary>
public class RunSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the folder containing the Screenplay files to run. Defaults to the current directory.
    /// </summary>
    [CommandArgument(0, "[PATH]")]
    [Description("Folder containing the Screenplay (.play) files to run. Defaults to the current directory.")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the cratis/stage image tag to run.
    /// </summary>
    [CommandOption("--tag <TAG>")]
    [Description("The cratis/stage image tag to run.")]
    [DefaultValue("latest")]
    public string Tag { get; set; } = "latest";

    /// <summary>
    /// Gets or sets the host port to publish the Stage API on.
    /// </summary>
    [CommandOption("--port <PORT>")]
    [Description("Host port to publish the Stage API on.")]
    [DefaultValue(9090)]
    public int Port { get; set; } = 9090;
}
