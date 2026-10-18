// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Streams;

/// <summary>
/// Settings identifying an event sequence and optional closed stream scope dimensions.
/// </summary>
public class StreamScopeSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the event sequence to inspect or repair.
    /// </summary>
    [CommandOption("--sequence <ID>")]
    [Description("Event sequence name (default: event-log)")]
    [DefaultValue(CliDefaults.DefaultEventSequenceId)]
    public string EventSequenceId { get; set; } = CliDefaults.DefaultEventSequenceId;

    /// <summary>
    /// Gets or sets the event source identifier dimension.
    /// </summary>
    [CommandOption("--event-source-id <ID>")]
    [Description("Event source identifier dimension of the closed scope")]
    public string? EventSourceId { get; set; }

    /// <summary>
    /// Gets or sets the event source type dimension.
    /// </summary>
    [CommandOption("--event-source-type <TYPE>")]
    [Description("Event source type dimension of the closed scope")]
    public string? EventSourceType { get; set; }

    /// <summary>
    /// Gets or sets the event stream type dimension.
    /// </summary>
    [CommandOption("--event-stream-type <TYPE>")]
    [Description("Event stream type dimension of the closed scope")]
    public string? EventStreamType { get; set; }

    /// <summary>
    /// Gets or sets the event stream identifier dimension.
    /// </summary>
    [CommandOption("--event-stream-id <ID>")]
    [Description("Event stream identifier dimension of the closed scope")]
    public string? EventStreamId { get; set; }
}
