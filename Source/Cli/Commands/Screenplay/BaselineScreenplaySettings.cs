// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Settings for checking a Screenplay release baseline.
/// </summary>
public class BaselineScreenplaySettings : GlobalSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaselineScreenplaySettings"/> class.
    /// </summary>
    public BaselineScreenplaySettings() => Output = OutputFormats.Plain;

    /// <summary>
    /// Gets or sets the model root containing persisted identities.
    /// </summary>
    [CommandArgument(0, "[ROOT]")]
    [Description("Model root containing .screenplay/identities.json. Defaults to the current directory, honoring the configured Screenplay MCP root.")]
    public string? Root { get; set; }
}
