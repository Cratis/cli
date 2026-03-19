// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Auth;

/// <summary>
/// Settings for the auth login command.
/// </summary>
public class LoginSettings : ChronicleSettings
{
    /// <summary>
    /// Gets or sets the username to log in with.
    /// </summary>
    [CommandArgument(0, "<USERNAME>")]
    [Description("The username to log in with")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the password for non-interactive login.
    /// </summary>
    [CommandOption("--password <PASSWORD>")]
    [Description("Password for non-interactive login. If omitted, prompts interactively.")]
    public string? Password { get; set; }
}
