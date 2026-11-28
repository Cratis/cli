// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Constraints;

/// <summary>
/// Settings for the constraints reindex command.
/// </summary>
public class ReindexConstraintsSettings : EventStoreSettings
{
    /// <summary>
    /// Gets or sets the event sequence whose constraint indexes are rebuilt.
    /// </summary>
    [CommandOption("--event-sequence <ID>")]
    [Description("Event sequence to rebuild constraint indexes from (default: event-log)")]
    [DefaultValue(CliDefaults.DefaultEventSequenceId)]
    public string EventSequenceId { get; set; } = CliDefaults.DefaultEventSequenceId;

    /// <summary>
    /// Gets or sets the names of the constraints to rebuild. Empty means every applicable unique and closing constraint.
    /// </summary>
    [CommandArgument(0, "[CONSTRAINT_NAME]")]
    [Description("Constraint names to rebuild. Omit to rebuild all applicable unique and closing constraints for the sequence.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1819:Properties should not return arrays", Justification = "Spectre.Console.Cli uses arrays for variadic arguments.")]
    public string[] ConstraintNames { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether to wait for the reindex job to finish.
    /// </summary>
    [CommandOption("--wait")]
    [Description("Wait for the reindex job to finish and exit non-zero unless it completed successfully")]
    public bool Wait { get; set; }
}
