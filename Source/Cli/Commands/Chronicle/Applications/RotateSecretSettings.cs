// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Applications;

/// <summary>
/// Settings for the rotate secret command.
/// </summary>
public class RotateSecretSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the application ID.
    /// </summary>
    [CommandArgument(0, "<APP_ID>")]
    [Description("Application identifier (GUID, from 'cratis applications list')")]
    public Guid AppId { get; set; }

    /// <summary>
    /// Gets or sets the new client secret.
    /// </summary>
    [CommandArgument(1, "<NEW_SECRET>")]
    [Description("The new client secret")]
    public string NewSecret { get; set; } = string.Empty;
}
