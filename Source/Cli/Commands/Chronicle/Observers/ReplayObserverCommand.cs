// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Replays an observer from the beginning.
/// </summary>
[CliCommand("replay", "Replay an observer from the beginning", Branch = typeof(ChronicleBranch.Observers), DynamicCompletion = "observers")]
[CliExample("chronicle", "observers", "replay", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOption("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional)")]
public class ReplayObserverCommand : ChronicleCommand<ObserverCommandSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ObserverCommandSettings settings, string format)
    {
        if (!ConfirmationHelper.ShouldProceed(settings, $"Are you sure you want to replay observer '{settings.ObserverId}'?"))
        {
            OutputFormatter.WriteMessage(format, "Aborted.");
            return ExitCodes.Success;
        }

        await services.Observers.Replay(new Replay
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = settings.EventSequenceId
        });

        OutputFormatter.WriteMessage(format, $"Replay started for observer '{settings.ObserverId}'. Use 'cratis observers show {settings.ObserverId}' to check progress.");
        return ExitCodes.Success;
    }
}
