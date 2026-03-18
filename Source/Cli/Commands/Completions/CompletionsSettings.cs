// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Completions;

/// <summary>
/// Settings for the completions command.
/// </summary>
public class CompletionsSettings : GlobalSettings
{
    /// <summary>
    /// Gets the target shell (bash, zsh, or fish).
    /// </summary>
    [CommandArgument(0, "<SHELL>")]
    [Description("Target shell: bash, zsh, or fish")]
    public string Shell { get; set; } = string.Empty;
}
