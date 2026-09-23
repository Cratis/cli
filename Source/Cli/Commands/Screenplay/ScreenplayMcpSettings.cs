// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Settings for the protocol-only Screenplay MCP server.
/// </summary>
public sealed class ScreenplayMcpSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets an explicit model directory instead of the project's configured root.
    /// </summary>
    [CommandArgument(0, "[PATH]")]
    [Description("Model directory. Defaults to the current project's configured root, or the current directory without project configuration.")]
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the project containing .cratis/ai.json.
    /// </summary>
    [CommandOption("--project-root")]
    [Description("Resolve the model root from this project's .cratis/ai.json.")]
    public string? ProjectRoot { get; set; }

    /// <summary>
    /// Gets or sets the host-provided environment variable containing the absolute project directory.
    /// </summary>
    [CommandOption("--project-root-env")]
    [Description("Read the project directory from a host-provided environment variable.")]
    public string? ProjectRootEnvironment { get; set; }
}
