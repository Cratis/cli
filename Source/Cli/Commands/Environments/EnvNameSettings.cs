// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Environments;

/// <summary>
/// Settings for commands that take an environment name.
/// </summary>
public class EnvNameSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the environment name.
    /// </summary>
    [CommandArgument(0, "<NAME>")]
    [Description("Environment name")]
    public string Name { get; set; } = string.Empty;
}
