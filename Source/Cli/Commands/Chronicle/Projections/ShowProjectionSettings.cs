// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Projections;

/// <summary>
/// Settings for the show projection command.
/// </summary>
public class ShowProjectionSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the projection identifier.
    /// </summary>
    [CommandArgument(0, "<IDENTIFIER>")]
    [Description("The projection identifier")]
    public string Identifier { get; set; } = string.Empty;
}
