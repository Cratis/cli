// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Grpc.Core;
using ClearPartitionQuarantineContract = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantine;

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Clears the quarantine of a single failed partition.
/// </summary>
/// <remarks>
/// A partition that exhausted its retry attempts is quarantined individually. <c language="text">retry-partition</c> refuses it and
/// <c language="text">clear-quarantine</c> only clears the observer-level quarantine, so this is the one way to release it. The kernel
/// resets the retry budget, keeps the attempt history, and by default starts the retry from the failed sequence number.
/// A retry re-runs the handler, so a side effect that already happened will happen again.
/// </remarks>
[LlmDescription("Clears the quarantine of one failed partition of an observer (after it exhausted its retry attempts), resets its retry budget and, unless --no-retry is given, starts a retry from the failed sequence number. A retry re-runs the handler, so side effects that already happened happen again. Requires Chronicle 19.29.0 or later. Prompts for confirmation unless --yes is specified. Exits non-zero when nothing was cleared.")]
[CommandEffect(CommandEffect.Mutating)]
[CliCommand("clear-partition-quarantine", "Clear the quarantine of a failed partition", Branch = typeof(ChronicleBranch.Observers), DynamicCompletion = "observers")]
[CliExample("chronicle", "observers", "clear-partition-quarantine", "550e8400-e29b-41d4-a716-446655440000", "my-partition")]
[CliExample("chronicle", "observers", "clear-partition-quarantine", "550e8400-e29b-41d4-a716-446655440000", "my-partition", "--no-retry")]
[LlmOption("<OBSERVER_ID>", "string", "Observer identifier (from 'cratis observers list') (positional)")]
[LlmOption("<PARTITION>", "string", "Partition key (typically an event source ID, from 'cratis failed-partitions list') (positional)")]
[LlmOption("--no-retry", "bool", "Only clear the quarantine; do not start a retry")]
public class ClearPartitionQuarantineCommand : ChronicleCommand<ClearPartitionQuarantineSettings>
{
    const string MinimumVersion = "19.29.0";

    /// <inheritdoc/>
    protected override string GetConfirmationPrompt(ClearPartitionQuarantineSettings settings) =>
        $"Are you sure you want to clear the quarantine of partition '{settings.Partition}' of observer '{settings.ObserverId}'?"
        + (settings.NoRetry ? string.Empty : " A retry will start and re-run the handler.");

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ClearPartitionQuarantineSettings settings, string format)
    {
        ClearPartitionQuarantineResponse response;
        try
        {
            response = await services.Observers.ClearPartitionQuarantine(new ClearPartitionQuarantineContract
            {
                EventStore = settings.ResolveEventStore(),
                Namespace = settings.ResolveNamespace(),
                ObserverId = settings.ObserverId,
                EventSequenceId = settings.EventSequenceId,
                Partition = settings.Partition,
                RetryImmediately = !settings.NoRetry
            });
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented)
        {
            OutputFormatter.WriteError(
                format,
                $"The connected Chronicle server does not support clearing a partition quarantine. This requires Chronicle {MinimumVersion} or later.",
                $"Upgrade the Chronicle server to {MinimumVersion} or later.",
                ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        switch (response.Outcome)
        {
            case ClearPartitionQuarantineOutcome.Cleared:
                return DescribeCleared(response, settings, format);

            case ClearPartitionQuarantineOutcome.NotFound:
                OutputFormatter.WriteError(
                    format,
                    $"Partition '{settings.Partition}' is not among the failed partitions of observer '{settings.ObserverId}', so nothing was cleared.",
                    $"List the failed partitions to check the key: cratis chronicle failed-partitions list --observer {settings.ObserverId}",
                    ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;

            case ClearPartitionQuarantineOutcome.NotQuarantined:
                OutputFormatter.WriteError(
                    format,
                    $"Partition '{settings.Partition}' of observer '{settings.ObserverId}' is failed but not quarantined, so nothing was cleared.",
                    $"Retry it instead: cratis chronicle observers retry-partition {settings.ObserverId} {settings.Partition}",
                    ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;

            default:
                OutputFormatter.WriteError(
                    format,
                    $"The quarantine of partition '{settings.Partition}' of observer '{settings.ObserverId}' was not cleared: {response.Outcome}.",
                    "Check the observer state with 'cratis chronicle observers show'.",
                    ExitCodes.ValidationErrorCode);
                return ExitCodes.ValidationError;
        }
    }

    static int DescribeCleared(ClearPartitionQuarantineResponse response, ClearPartitionQuarantineSettings settings, string format)
    {
        var target = $"partition '{settings.Partition}' of observer '{settings.ObserverId}'";

        if (settings.NoRetry)
        {
            OutputFormatter.WriteMessage(format, $"Quarantine cleared for {target}; the retry budget was reset and no retry was started. Use 'cratis chronicle observers retry-partition {settings.ObserverId} {settings.Partition}' to retry it.");
            return ExitCodes.Success;
        }

        if (response.RetryOutcome == PartitionRecoveryOutcome.Started)
        {
            OutputFormatter.WriteMessage(format, $"Quarantine cleared and retry started for {target}. Use 'cratis chronicle observers show {settings.ObserverId}' to check progress.");
            return ExitCodes.Success;
        }

        if (response.RetryOutcome == PartitionRecoveryOutcome.ObserverQuarantined)
        {
            OutputFormatter.WriteError(
                format,
                $"Quarantine cleared for {target}, but the retry was not started because observer '{settings.ObserverId}' is itself quarantined.",
                $"Clear the observer quarantine (cratis chronicle observers clear-quarantine {settings.ObserverId}), then retry: cratis chronicle observers retry-partition {settings.ObserverId} {settings.Partition}",
                ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        OutputFormatter.WriteError(
            format,
            $"Quarantine cleared for {target}, but the retry was not started: {response.RetryOutcome}.",
            $"Retry it: cratis chronicle observers retry-partition {settings.ObserverId} {settings.Partition}",
            ExitCodes.ValidationErrorCode);
        return ExitCodes.ValidationError;
    }
}
