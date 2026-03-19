// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Auth;

/// <summary>
/// Settings for the auth login command.
/// </summary>
public class LoginSettings : ChronicleSettings
{
    /// <summary>
    /// Gets or sets the client identifier to authenticate with.
    /// </summary>
    [CommandArgument(0, "<CLIENT_ID>")]
    [Description("The client identifier to authenticate with (registered via 'cratis chronicle applications add')")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client secret for non-interactive login.
    /// </summary>
    [CommandOption("--secret <SECRET>")]
    [Description("Client secret for non-interactive login. If omitted, prompts interactively.")]
    public string? Secret { get; set; }
}
