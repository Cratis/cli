// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Users;

/// <summary>
/// Settings for the remove user command.
/// </summary>
public class RemoveUserSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the user ID to remove.
    /// </summary>
    [CommandArgument(0, "<USER_ID>")]
    [Description("User identifier (GUID, from 'cratis users list')")]
    public Guid UserId { get; set; }
}
