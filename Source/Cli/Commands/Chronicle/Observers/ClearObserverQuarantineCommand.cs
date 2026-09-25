// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ClearObserverQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearObserverQuarantine;

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Clears quarantine for an observer.
/// </summary>
/// <remarks>
/// This used to look the contract method up by reflection and report every miss as "the connected kernel does not
/// support this", which is how it came to fail against every kernel that does: the gRPC client is a generated proxy
/// implementing the interface explicitly, and <see cref="Type.GetMethods()"/> does not return explicit interface
/// implementations. The suggestion it printed - upgrade the contracts - was unfollowable, because there was no version
/// where the lookup would have succeeded. The CLI compiles against the contracts that declare the method, so calling
/// it directly is both simpler and the only way to tell a kernel that cannot do this from a lookup that went wrong.
/// </remarks>
[LlmDescription("Clears quarantine for a quarantined observer so it can resume processing. Prompts for confirmation unless --yes is specified.")]
[CommandEffect(CommandEffect.Mutating)]
[CliCommand("clear-quarantine", "Clear quarantine for an observer", Branch = typeof(ChronicleBranch.Observers), DynamicCompletion = "observers")]
[CliExample("chronicle", "observers", "clear-quarantine", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOption("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional)")]
public class ClearObserverQuarantineCommand : ChronicleCommand<ObserverCommandSettings>
{
    /// <inheritdoc/>
    protected override string GetConfirmationPrompt(ObserverCommandSettings settings) =>
        $"Are you sure you want to clear quarantine for observer '{settings.ObserverId}'?";

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ObserverCommandSettings settings, string format)
    {
        await services.Observers.ClearObserverQuarantine(new ClearObserverQuarantineContract
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = settings.EventSequenceId
        });

        OutputFormatter.WriteMessage(format, $"Quarantine cleared for observer '{settings.ObserverId}'.");
        return ExitCodes.Success;
    }
}
