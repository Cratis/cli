// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Settings for the report-error command.
/// </summary>
public class ReportErrorSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the issue title.
    /// </summary>
    [CommandOption("--title <TEXT>")]
    [Description("Issue title (prompted interactively if not provided)")]
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the issue body.
    /// </summary>
    [CommandOption("--body <TEXT>")]
    [Description("Issue description body (prompted interactively if not provided)")]
    public string? Body { get; set; }
}
