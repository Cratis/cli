// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Streams;

/// <summary>
/// Settings for repairing an exact manual stream closure.
/// </summary>
public class ReopenClosedStreamSettings : StreamScopeSettings
{
    /// <summary>
    /// Gets or sets the reason recorded in the repair audit event.
    /// </summary>
    [CommandOption("--reason <TEXT>")]
    [Description("Required reason for repairing the manual closure; recorded in the system event sequence")]
    public string Reason { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Reason))
        {
            return ValidationResult.Error("A non-empty --reason is required to reopen a closed scope.");
        }

        if (string.IsNullOrWhiteSpace(EventSourceId) && string.IsNullOrWhiteSpace(EventSourceType) &&
            string.IsNullOrWhiteSpace(EventStreamType) && string.IsNullOrWhiteSpace(EventStreamId))
        {
            return ValidationResult.Error("Specify at least one scope dimension: --event-source-id, --event-source-type, --event-stream-type, or --event-stream-id.");
        }

        return base.Validate();
    }
}
