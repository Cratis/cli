// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Settings for comparing an authored model with what a Chronicle namespace has observed.
/// </summary>
public class ConformNamespaceScreenplaySettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the authored root document or model folder.
    /// </summary>
    [CommandArgument(0, "<MODEL_ROOT>")]
    [Description("Authored .play root file (including imports), or folder containing one application.")]
    public string ModelRoot { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the event sequence to observe.
    /// </summary>
    [CommandOption("--sequence <ID>")]
    [Description("Event sequence to observe (default: event-log)")]
    [DefaultValue(CliDefaults.DefaultEventSequenceId)]
    public string EventSequenceId { get; set; } = CliDefaults.DefaultEventSequenceId;

    /// <summary>
    /// Gets or sets the first sequence number to examine.
    /// </summary>
    [CommandOption("--from <NUMBER>")]
    [Description("First sequence number to examine (default: the first event). Starting later gives partial coverage.")]
    public ulong? From { get; set; }

    /// <summary>
    /// Gets or sets the last sequence number to examine.
    /// </summary>
    [CommandOption("--to <NUMBER>")]
    [Description("Last sequence number to examine (default: the tail captured when the scan starts).")]
    public ulong? To { get; set; }

    /// <summary>
    /// Gets or sets the most events to examine.
    /// </summary>
    [CommandOption("--max-events <COUNT>")]
    [Description("Most events to examine (default: 100000). Reaching it records partial coverage, not an error.")]
    [DefaultValue(NamespaceObservationScan.DefaultMaxEvents)]
    public int MaxEvents { get; set; } = NamespaceObservationScan.DefaultMaxEvents;

    /// <summary>
    /// Gets or sets a value indicating whether drift, or an inconclusive comparison, fails the command.
    /// </summary>
    [CommandOption("--fail-on-drift")]
    [Description("Exit 7 (drift_detected) on known drift and 8 (inconclusive) when drift can be neither established nor ruled out.")]
    public bool FailOnDrift { get; set; }
}
