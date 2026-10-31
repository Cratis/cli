// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Explicit operator-attested scope for replaying an existing outbox observer.
/// </summary>
public sealed class ReplayExistingConsumerSettings : ChronicleSettings
{
    /// <summary>
    /// Gets or sets the existing producer observer identifier.
    /// </summary>
    [CommandArgument(0, "<OBSERVER_ID>")]
    [Description("Existing producer outbox observer identifier (operator-provided, UNVERIFIED)")]
    public string ObserverId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the explicit producer event store.
    /// </summary>
    [CommandOption("-e|--event-store <NAME>")]
    [Description("Explicit producer event store (required; no context default)")]
    public string EventStore { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the explicit producer namespace.
    /// </summary>
    [CommandOption("-n|--namespace <NAME>")]
    [Description("Explicit producer namespace (required; no context default)")]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the explicit outbox sequence.
    /// </summary>
    [CommandOption("--sequence <ID>")]
    [Description("Explicit event sequence; must be exactly 'outbox'")]
    public string EventSequenceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unverified subscription label.
    /// </summary>
    [CommandOption("--subscription-id <ID>")]
    [Description("Operator-attested subscription label (UNVERIFIED; does not control delivery)")]
    public string SubscriptionId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unverified consumer label.
    /// </summary>
    [CommandOption("--consumer-identity <LABEL>")]
    [Description("Operator-attested consumer label (UNVERIFIED; does not control delivery)")]
    public string ConsumerIdentity { get; set; } = string.Empty;

    /// <inheritdoc/>
    public override ValidationResult Validate()
    {
        if (new[] { ObserverId, EventStore, Namespace, EventSequenceId, SubscriptionId, ConsumerIdentity }.Any(string.IsNullOrWhiteSpace))
        {
            return ValidationResult.Error("Explicit observer, producer event store, namespace, sequence, subscription-id and consumer-identity are required.");
        }

        if (!string.Equals(EventSequenceId, "outbox", StringComparison.Ordinal))
        {
            return ValidationResult.Error("--sequence must be exactly 'outbox'.");
        }

        return base.Validate();
    }
}
