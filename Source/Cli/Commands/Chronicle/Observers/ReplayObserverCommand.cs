// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Replays an observer from the beginning.
/// </summary>
[LlmDescription("Replays an observer from the beginning of the event log, resetting all state it has built. Destructive — prompts for confirmation unless --yes is specified.")]
[CommandEffect(CommandEffect.Destructive)]
[CliCommand("replay", "Replay an observer from the beginning", Branch = typeof(ChronicleBranch.Observers), DynamicCompletion = "observers")]
[CliExample("chronicle", "observers", "replay", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOption("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis chronicle observers list') (positional)")]
public class ReplayObserverCommand : ChronicleCommand<ObserverCommandSettings>
{
    internal static async Task<int> ExecuteReplayAsync(IServices services, string eventStore, string @namespace, string eventSequenceId, string observerId, string format)
    {
        await services.Observers.Replay(new Replay
        {
            EventStore = eventStore,
            Namespace = @namespace,
            ObserverId = observerId,
            EventSequenceId = eventSequenceId
        });

        OutputFormatter.WriteMessage(format, $"Replay started for observer '{observerId}'. Use 'cratis chronicle observers show {observerId}' to check progress.");
        return ExitCodes.Success;
    }

    /// <inheritdoc/>
    protected override string GetConfirmationPrompt(ObserverCommandSettings settings) =>
        $"Are you sure you want to replay observer '{settings.ObserverId}'?";

    /// <inheritdoc/>
    protected override Task<int> ExecuteCommandAsync(IServices services, ObserverCommandSettings settings, string format) =>
        ExecuteReplayAsync(services, settings.ResolveEventStore(), settings.ResolveNamespace(), settings.EventSequenceId, settings.ObserverId, format);
}
