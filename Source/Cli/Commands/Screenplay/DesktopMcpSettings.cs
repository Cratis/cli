// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Ai;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Settings for inspecting the Screenplay desktop MCP distribution.
/// </summary>
public class DesktopMcpSettings : CommandSettings
{
    /// <summary>
    /// Gets or sets the desktop clients to manage.
    /// </summary>
    [CommandOption("--clients <CLIENTS>")]
    [Description("Select claude,chatgpt (one or both). Noninteractive install/update/uninstall requires an explicit selection.")]
    public string? Clients { get; set; }

    /// <summary>
    /// Gets or sets the Screenplay semantic release version.
    /// </summary>
    [CommandOption("--version <VERSION>")]
    [Description("Pin a Screenplay semantic release version without a leading v. Defaults to the latest stable release.")]
    public string? Version { get; set; }

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        try
        {
            if (Version is not null) DesktopMcpArtifacts.ValidateVersion(Version);
            return ValidationResult.Success();
        }
        catch (AiMcpConfigurationInvalid exception)
        {
            return ValidationResult.Error(exception.Message);
        }
    }
}

/// <summary>
/// Settings for previewing or removing a Screenplay desktop MCP distribution.
/// </summary>
public class DesktopMcpRemovalSettings : DesktopMcpSettings
{
    /// <summary>
    /// Gets or sets whether to preview without downloading artifacts, writing files, or opening a host.
    /// </summary>
    [CommandOption("--dry-run")]
    [Description("Validate and preview without downloading artifacts, writing files, or opening a host. Release metadata may still be queried.")]
    public bool DryRun { get; set; }
}

/// <summary>
/// Settings for installing or updating a Screenplay desktop MCP distribution.
/// </summary>
public class DesktopMcpTargetSettings : DesktopMcpRemovalSettings
{
    /// <summary>
    /// Gets or sets the existing physical model folder for the ChatGPT source.
    /// </summary>
    [CommandOption("--model-root <DIRECTORY>")]
    [Description("Existing physical model folder for ChatGPT. Claude asks in its install dialog. Update retains the previous ChatGPT folder when omitted.")]
    public string? ModelRoot { get; set; }
}
