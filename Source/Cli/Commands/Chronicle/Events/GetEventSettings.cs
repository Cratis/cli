// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Events;

/// <summary>
/// Settings for the get-event-by-sequence-number command.
/// </summary>
public class GetEventSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the event sequence number to retrieve.
    /// </summary>
    [CommandArgument(0, "<SEQUENCE_NUMBER>")]
    [Description("Event sequence number to retrieve")]
    public ulong SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the event sequence ID to query.
    /// </summary>
    [CommandOption("--sequence <ID>")]
    [Description("Event sequence name (default: event-log)")]
    [DefaultValue(CliDefaults.DefaultEventSequenceId)]
    public string EventSequenceId { get; set; } = CliDefaults.DefaultEventSequenceId;
}
