// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.FailedPartitions;

/// <summary>
/// Settings for the show failed partition command.
/// </summary>
public class ShowFailedPartitionSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the observer ID.
    /// </summary>
    [CommandArgument(0, "<OBSERVER_ID>")]
    [Description("Observer identifier (from 'cratis failed-partitions list')")]
    public string ObserverId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the partition key.
    /// </summary>
    [CommandArgument(1, "<PARTITION>")]
    [Description("Partition key (typically an event source ID, from 'cratis failed-partitions list')")]
    public string Partition { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to show full stack traces and all attempts.
    /// </summary>
    [CommandOption("--detailed")]
    [Description("Show full stack traces and all attempts")]
    [DefaultValue(false)]
    public bool Detailed { get; set; }
}
