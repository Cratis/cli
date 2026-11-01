// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Settings for commands that act on one configuration key.
/// </summary>
public class ConfigKeySettings : EnvironmentSettings
{
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    [CommandArgument(0, "<KEY>")]
    [Description("Configuration key")]
    public string Key { get; set; } = string.Empty;
}
