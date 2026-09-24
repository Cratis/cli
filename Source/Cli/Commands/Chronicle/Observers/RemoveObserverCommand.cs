// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using RemoveObserverContract = Cratis.Chronicle.Contracts.Observation.RemoveObserver;

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Removes an observer and everything the event store keeps for it.
/// </summary>
/// <remarks>
/// For the observer whose declaring code is gone - a read model and its projection that were deleted, a reactor that
/// was removed. Nothing tells the event store, so the observer settles into Disconnected and keeps its records
/// forever. The kernel refuses while the observer is running or still has a subscribed client, so this cannot be used
/// to tear a live observer out from under a running application, and there is deliberately no force flag.
/// </remarks>
[LlmDescription("Removes an observer and everything the event store keeps for it - its definition, state, handled counts, failed partitions and, for a projection, its projection definition - across every namespace of the event store. Read model data is not touched. Refused while the observer is running or still has a subscribed client. Prompts for confirmation unless --yes is specified.")]
[CommandEffect(CommandEffect.Destructive)]
[CliCommand("remove", "Remove an observer and everything kept for it", Branch = typeof(ChronicleBranch.Observers), DynamicCompletion = "observers")]
[CliExample("chronicle", "observers", "remove", "550e8400-e29b-41d4-a716-446655440000")]
[LlmOption("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis chronicle observers list') (positional)")]
public class RemoveObserverCommand : ChronicleCommand<ObserverCommandSettings>
{
    /// <inheritdoc/>
    /// <remarks>
    /// The prompt says what goes, what stays and that it reaches every namespace. Removal cannot be undone, and the
    /// difference between it and a replay - which an operator reaching for it may well have meant - is that a
    /// re-registered observer starts over from the beginning of the sequence.
    /// </remarks>
    protected override string GetConfirmationPrompt(ObserverCommandSettings settings) =>
        string.Join(
            Environment.NewLine,
            [
                $"Remove observer '{settings.ObserverId}' and everything the event store keeps for it?",
                string.Empty,
                "This deletes its definition, its state and handled counts in every namespace of event store",
                $"'{settings.ResolveEventStore()}', its failed partitions and, if it is a projection, its projection",
                "definition. Read models and their data are not touched.",
                string.Empty,
                "This cannot be undone. The observer comes back only if the code that declared it registers it again,",
                "and it will then start over from the beginning of the event sequence.",
                string.Empty,
                "Are you sure?"
            ]);

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ObserverCommandSettings settings, string format)
    {
        var response = await services.Observers.RemoveObserver(new RemoveObserverContract
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = settings.EventSequenceId
        });

        if (response.Outcome == ObserverRemovalOutcome.Removed)
        {
            OutputFormatter.WriteMessage(format, $"Observer '{settings.ObserverId}' was removed from event store '{settings.ResolveEventStore()}'.");
            return ExitCodes.Success;
        }

        var (message, suggestion, exitCode) = DescribeRefusal(response, settings);
        OutputFormatter.WriteError(format, message, suggestion, ExitCodes.CodeFor(exitCode));
        return exitCode;
    }

    static (string Message, string Suggestion, int ExitCode) DescribeRefusal(RemoveObserverResponse response, ObserverCommandSettings settings)
    {
        var @namespace = string.IsNullOrEmpty(response.BlockingNamespace) ? settings.ResolveNamespace() : response.BlockingNamespace;

        return response.Outcome switch
        {
            ObserverRemovalOutcome.ObserverActive =>
                ($"Observer '{settings.ObserverId}' is still running in namespace '{@namespace}', so it was not removed.",
                 "Stop the application that declares this observer, then try again.",
                 ExitCodes.ValidationError),

            ObserverRemovalOutcome.ObserverSubscribed =>
                ($"Observer '{settings.ObserverId}' still has a client subscribed to it in namespace '{@namespace}', so it was not removed.",
                 "Stop the application that declares this observer, then try again.",
                 ExitCodes.ValidationError),

            ObserverRemovalOutcome.ObserverNotFound =>
                ($"Observer '{settings.ObserverId}' is not registered in event store '{settings.ResolveEventStore()}', so there was nothing to remove.",
                 "List the observers to check the identifier: cratis chronicle observers list",
                 ExitCodes.NotFound),

            _ =>
                ($"Observer '{settings.ObserverId}' was not removed: {response.Outcome}.",
                 "Check the observer state with 'cratis chronicle observers show'.",
                 ExitCodes.ValidationError)
        };
    }
}
