// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.FailedPartitions;

/// <summary>
/// Settings for the list failed partitions command.
/// </summary>
public class ListFailedPartitionsSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets an optional observer ID filter.
    /// </summary>
    [CommandOption("--observer <ID>")]
    [Description("Filter by observer ID")]
    public string? ObserverId { get; set; }
}
