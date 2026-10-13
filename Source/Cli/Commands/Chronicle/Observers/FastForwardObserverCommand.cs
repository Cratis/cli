// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Grpc.Core;
using FastForwardContract = Cratis.Chronicle.Contracts.Observation.FastForward;

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Fast forwards an observer to the tail of its event sequence.
/// </summary>
/// <remarks>
/// Every event the observer has not yet handled is skipped without invoking it, and its failed partitions are
/// discarded. Use it to get past a backlog or events that can never be handled when the read side does not need to
/// be rebuilt. Unlike <c language="text">replay</c> it does not reset any state the observer has built.
/// </remarks>
[LlmDescription("Fast forwards an observer to the tail of its event sequence: every event it has not yet handled is skipped without being observed and its failed partitions are discarded. State already built is kept, so read models will miss the effect of the skipped events; use replay to rebuild them. Requires Chronicle 19.56.0 or later. Destructive — prompts for confirmation unless --yes is specified. Exits non-zero when the observer could not be fast forwarded.")]
[CommandEffect(CommandEffect.Destructive)]
[CliCommand("fast-forward", "Fast forward an observer to the end of its event sequence", Branch = typeof(ChronicleBranch.Observers), DynamicCompletion = "observers")]
[CliExample("chronicle", "observers", "fast-forward", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOption("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional)")]
public class FastForwardObserverCommand : ChronicleCommand<ObserverCommandSettings>
{
    const string MinimumVersion = "19.56.0";

    /// <inheritdoc/>
    protected override string GetConfirmationPrompt(ObserverCommandSettings settings) =>
        $"Are you sure you want to fast forward observer '{settings.ObserverId}'? All events it has not yet handled will be skipped.";

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ObserverCommandSettings settings, string format)
    {
        FastForwardResponse response;
        try
        {
            response = await services.Observers.FastForward(new FastForwardContract
            {
                EventStore = settings.ResolveEventStore(),
                Namespace = settings.ResolveNamespace(),
                ObserverId = settings.ObserverId,
                EventSequenceId = settings.EventSequenceId
            });
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented)
        {
            OutputFormatter.WriteError(
                format,
                $"The connected Chronicle server does not support fast forwarding observers. This requires Chronicle {MinimumVersion} or later.",
                $"Upgrade the Chronicle server to {MinimumVersion} or later.",
                ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        switch (response.Outcome)
        {
            case FastForwardOutcome.FastForwarded:
                OutputFormatter.WriteMessage(format, $"Observer '{settings.ObserverId}' fast forwarded from event {response.PreviousNextEventSequenceNumber} to {response.NextEventSequenceNumber}.");
                return ExitCodes.Success;

            case FastForwardOutcome.AlreadyAtTail:
                OutputFormatter.WriteMessage(format, $"Observer '{settings.ObserverId}' is already at the end of the event sequence; nothing was skipped.");
                return ExitCodes.Success;

            case FastForwardOutcome.Replaying:
                OutputFormatter.WriteError(
                    format,
                    $"Observer '{settings.ObserverId}' is replaying and cannot be fast forwarded until the replay has completed.",
                    $"Check progress with 'cratis chronicle observers show {settings.ObserverId}'.",
                    ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;

            default:
                OutputFormatter.WriteError(
                    format,
                    $"Observer '{settings.ObserverId}' was not fast forwarded: {response.Outcome}.",
                    "Check the observer state with 'cratis chronicle observers show'.",
                    ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
        }
    }
}
