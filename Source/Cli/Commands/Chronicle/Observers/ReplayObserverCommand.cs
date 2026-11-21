// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Replays an observer from the beginning.
/// </summary>
/// <remarks>
/// Replaying a subscription (outbox forwarding) observer can forward retained producer events into the consumer
/// Inbox again. Such a replay requires an explicit producer event store and namespace, and a confirmation of the
/// duplicate-delivery warning, before any replay is started.
/// </remarks>
[LlmDescription("Replays an observer from the beginning of the event log, resetting all state it has built. Destructive — prompts for confirmation unless --yes is specified. Replaying a subscription (outbox forwarding) observer, identified as '<subscription-id>-><consumer-store>' or by --sequence outbox, requires explicit -e/--event-store and -n/--namespace for the producer and warns that it may append duplicate events to the consumer Inbox.")]
[CommandEffect(CommandEffect.Destructive)]
[CliCommand("replay", "Replay an observer from the beginning", Branch = typeof(ChronicleBranch.Observers), DynamicCompletion = "observers")]
[CliExample("chronicle", "observers", "replay", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOption("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis chronicle observers list') (positional)")]
public class ReplayObserverCommand : ChronicleCommand<ObserverCommandSettings>
{
    /// <summary>
    /// The event sequence a subscription forwarding observer reads from.
    /// </summary>
    internal const string OutboxEventSequenceId = "outbox";

    /// <summary>
    /// The separator between the subscription and the consumer event store in a forwarding observer identifier.
    /// </summary>
    internal const string ForwardingObserverSeparator = "->";

    /// <summary>
    /// Determines whether a replay targets a subscription forwarding observer.
    /// </summary>
    /// <param name="observerId">The observer identifier.</param>
    /// <param name="eventSequenceId">The event sequence the observer reads from.</param>
    /// <returns><see langword="true"/> when the observer forwards outbox events to a consumer; otherwise <see langword="false"/>.</returns>
    internal static bool IsSubscriptionReplay(string observerId, string eventSequenceId) =>
        observerId.Contains(ForwardingObserverSeparator, StringComparison.Ordinal) ||
        string.Equals(eventSequenceId, OutboxEventSequenceId, StringComparison.Ordinal);

    /// <summary>
    /// Gets the duplicate-delivery warning shown before replaying a subscription forwarding observer.
    /// </summary>
    /// <param name="settings">The command settings.</param>
    /// <returns>The confirmation prompt.</returns>
    internal static string GetSubscriptionReplayPrompt(ObserverCommandSettings settings) =>
        $"Replaying subscription observer '{settings.ObserverId}' in producer event store '{settings.EventStore}', namespace '{settings.Namespace}' forwards retained outbox events again and may append duplicate events to the consumer Inbox. Are you sure you want to replay it?";

    /// <inheritdoc/>
    protected override int? ExecuteOffline(ObserverCommandSettings settings) =>
        IsSubscriptionReplay(settings.ObserverId, settings.EventSequenceId)
            ? ValidateExplicitScope(settings, settings.ResolveOutputFormat())
            : null;

    /// <inheritdoc/>
    protected override string GetConfirmationPrompt(ObserverCommandSettings settings) =>
        IsSubscriptionReplay(settings.ObserverId, settings.EventSequenceId)
            ? GetSubscriptionReplayPrompt(settings)
            : $"Are you sure you want to replay observer '{settings.ObserverId}'?";

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ObserverCommandSettings settings, string format)
    {
        var eventSequenceId = await settings.ResolveEventSequenceId(services.Observers);
        if (RequiresSubscriptionConfirmationAfterResolving(settings, eventSequenceId) &&
            (ValidateExplicitScope(settings, format) ?? RequireConfirmedSubscriptionReplay(settings, format)) is { } exitCode)
        {
            return exitCode;
        }

        await services.Observers.Replay(new Replay
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = eventSequenceId
        });

        OutputFormatter.WriteMessage(format, $"Replay started for observer '{settings.ObserverId}'. Use 'cratis chronicle observers show {settings.ObserverId}' to check progress.");
        return ExitCodes.Success;
    }

    static bool RequiresSubscriptionConfirmationAfterResolving(ObserverCommandSettings settings, string eventSequenceId) =>
        !IsSubscriptionReplay(settings.ObserverId, settings.EventSequenceId) &&
        IsSubscriptionReplay(settings.ObserverId, eventSequenceId);

    static int? ValidateExplicitScope(ObserverCommandSettings settings, string format)
    {
        if (!string.IsNullOrWhiteSpace(settings.EventStore) && !string.IsNullOrWhiteSpace(settings.Namespace))
        {
            return null;
        }

        OutputFormatter.WriteError(
            format,
            $"Replaying subscription observer '{settings.ObserverId}' requires an explicit producer event store and namespace",
            "Re-run the command with -e/--event-store <PRODUCER_STORE> and -n/--namespace <NAMESPACE>",
            ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }

    static int? RequireConfirmedSubscriptionReplay(ObserverCommandSettings settings, string format)
    {
        // The earlier prompt did not show the duplicate-delivery warning, and prompting is not possible while connected.
        if (settings.Yes)
        {
            return null;
        }

        OutputFormatter.WriteError(
            format,
            $"Observer '{settings.ObserverId}' is a subscription observer; replaying it may append duplicate events to the consumer Inbox",
            $"Re-run the command with --sequence {OutboxEventSequenceId} to review the warning, or with --yes to confirm it",
            ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }
}
