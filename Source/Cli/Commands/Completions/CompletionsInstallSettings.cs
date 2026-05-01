// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Completions;

/// <summary>
/// Settings for the completions install command.
/// </summary>
public class CompletionsInstallSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the shell to install for. When omitted, auto-detected via the <c>$SHELL</c> environment variable.
    /// </summary>
    [CommandOption("--shell <SHELL>")]
    [Description("Shell to install for: bash, zsh, or fish. Defaults to auto-detection via $SHELL.")]
    public string? Shell { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to remove and re-add the completions line even if already configured.
    /// </summary>
    [CommandOption("--force")]
    [Description("Remove and re-add the completions line even if already configured.")]
    public bool Force { get; set; }
}
