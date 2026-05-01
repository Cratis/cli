// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Context;

/// <summary>
/// Settings for the set-value command.
/// </summary>
public class SetValueSettings : GlobalSettings
{
    /// <summary>
    /// Gets or sets the configuration key.
    /// </summary>
    [CommandArgument(0, "<KEY>")]
    [Description("Configuration key (server, event-store, namespace, client-id, client-secret, management-port)")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the configuration value.
    /// </summary>
    [CommandArgument(1, "<VALUE>")]
    [Description("The value to set")]
    public string Value { get; set; } = string.Empty;
}
