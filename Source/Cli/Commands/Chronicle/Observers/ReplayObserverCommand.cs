// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Replays an observer from the beginning.
/// </summary>
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
