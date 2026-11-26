// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Affected;

/// <summary>
/// Settings for listing the test projects.
/// </summary>
public class AffectedProjectsSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the ref to read the projects from.
    /// </summary>
    [CommandOption("--to <REF>")]
    [Description("The ref to read the test projects from. Default: HEAD")]
    [DefaultValue("HEAD")]
    public string To { get; set; } = "HEAD";
}
