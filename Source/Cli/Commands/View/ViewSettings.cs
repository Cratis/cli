// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.View;

/// <summary>
/// Settings for the view command.
/// </summary>
public class ViewSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the project file, or the folder holding it, to view. Defaults to the current directory.
    /// </summary>
    [CommandArgument(0, "[PATH]")]
    [Description("Project file (.csproj), or the folder holding exactly one, to view. Defaults to the current directory.")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the build configuration whose output is read.
    /// </summary>
    [CommandOption("-c|--configuration <CONFIGURATION>")]
    [Description("The build configuration whose output assembly is read for embedded Screenplay documents.")]
    [DefaultValue("Debug")]
    public string Configuration { get; set; } = "Debug";

    /// <summary>
    /// Gets or sets the target framework to read, for a project that targets several.
    /// </summary>
    [CommandOption("-f|--framework <FRAMEWORK>")]
    [Description("The target framework to read, for a project that targets several. Defaults to the first one it lists.")]
    public string? Framework { get; set; }

    /// <summary>
    /// Gets or sets the local port the viewer is served on.
    /// </summary>
    [CommandOption("--port <PORT>")]
    [Description("The local port to serve the viewer on. 0 picks a free port.")]
    [DefaultValue(0)]
    public int Port { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether documents are generated from source even when the assembly embeds some.
    /// </summary>
    [CommandOption("--from-source")]
    [Description("Generate the documents from the project's source even when its output assembly embeds Screenplay documents.")]
    [DefaultValue(false)]
    public bool FromSource { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether opening a browser is skipped.
    /// </summary>
    [CommandOption("--no-browser")]
    [Description("Serve the viewer without opening a browser window.")]
    [DefaultValue(false)]
    public bool NoBrowser { get; set; }

    /// <inheritdoc/>
    public override ValidationResult Validate() =>
        Port is < 0 or > 65535
            ? ValidationResult.Error("--port must be between 0 and 65535")
            : base.Validate();
}
