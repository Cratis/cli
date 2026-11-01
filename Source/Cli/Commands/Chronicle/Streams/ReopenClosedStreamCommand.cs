// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Sequences;
using Grpc.Core;

namespace Cratis.Cli.Commands.Chronicle.Streams;

/// <summary>
/// Repairs an exact manual stream closure, recording the authenticated actor and reason.
/// </summary>
[LlmDescription("Repairs a mistaken manual stream closure matching exactly the supplied dimensions; never widens, narrows or splits a closure. Requires a non-empty --reason and an authenticated principal with a subject. Records a StreamScopeReopened system event before removing the closure. Event-driven closures must instead be reopened by their ReopenedBy events. Requires Chronicle 19.39.0 or later. Prompts for confirmation unless --yes is specified. Exits non-zero when nothing was reopened.")]
[CommandEffect(CommandEffect.Mutating)]
[CliCommand("reopen", "Repair an exact manual stream closure", Branch = typeof(ChronicleBranch.Streams.Closed))]
[CliExample("chronicle", "streams", "closed", "reopen", "--event-source-id", "order-42", "--reason", "Closed the wrong order")]
[LlmOption("--sequence", "string", "Event sequence name (default: event-log)")]
[LlmOption("--event-source-id", "string", "Exact closure's event source identifier dimension")]
[LlmOption("--event-source-type", "string", "Exact closure's event source type dimension")]
[LlmOption("--event-stream-type", "string", "Exact closure's event stream type dimension")]
[LlmOption("--event-stream-id", "string", "Exact closure's event stream identifier dimension")]
[LlmOption("--reason", "string", "Required repair reason recorded in the system event sequence")]
public class ReopenClosedStreamCommand : ChronicleCommand<ReopenClosedStreamSettings>
{
    /// <inheritdoc/>
    protected override string GetConfirmationPrompt(ReopenClosedStreamSettings settings) =>
        $"Reopen the exact manual closure in event store '{settings.ResolveEventStore()}', namespace '{settings.ResolveNamespace()}', sequence '{settings.EventSequenceId}' " +
        $"(event source id: {settings.EventSourceId ?? "<unset>"}, event source type: {settings.EventSourceType ?? "<unset>"}, " +
        $"event stream type: {settings.EventStreamType ?? "<unset>"}, event stream id: {settings.EventStreamId ?? "<unset>"})? Reason: {settings.Reason}";

    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ReopenClosedStreamSettings settings, string format)
    {
        CommandResult<ReopenStreamScopeResponse> result;
        try
        {
            result = await services.Sequences.ReopenStreamScope(new ReopenStreamScopeRequest
            {
                EventStore = settings.ResolveEventStore(),
                Namespace = settings.ResolveNamespace(),
                EventSequenceId = settings.EventSequenceId,
                Reason = settings.Reason,
                EventSourceId = settings.EventSourceId,
                EventSourceType = settings.EventSourceType,
                EventStreamType = settings.EventStreamType,
                EventStreamId = settings.EventStreamId
            });
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented)
        {
            OutputFormatter.WriteError(format, "Reopening a closed stream scope requires Chronicle 19.39.0 or later.", "Upgrade the Chronicle server to 19.39.0 or later.", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        if (HandleCommandResult(result, format) is { } exitCode)
        {
            return exitCode;
        }

        var response = result.Response;
        if (response.IsSuccess && response.Error == ReopenStreamScopeError.None)
        {
            OutputFormatter.WriteMessage(format, "The exact manual stream closure was reopened; the reason and authenticated actor were recorded in a StreamScopeReopened system event. Other covering closures may still prevent appends. Use 'cratis chronicle streams closed list' to inspect remaining closures.");
            return ExitCodes.Success;
        }

        var (message, suggestion) = response.Error switch
        {
            ReopenStreamScopeError.NotCompleted => (
                "No exact manual closure matches the supplied dimensions, so nothing was reopened.",
                "Use 'cratis chronicle streams closed list' to find the exact closure; supply all and only its dimensions."),
            ReopenStreamScopeError.ClosedByEvent => (
                "The scope was closed by a closing event, so nothing was reopened.",
                "Append one of the closing constraint's ReopenedBy events instead; use 'cratis chronicle streams closed list' to identify its owning constraint."),
            _ => (
                $"The stream scope was not reopened: {response.Error} (IsSuccess: {response.IsSuccess}).",
                "Inspect 'cratis chronicle streams closed list' and the server logs before attempting another repair.")
        };
        OutputFormatter.WriteError(format, message, suggestion, ExitCodes.ValidationErrorCode);

        return ExitCodes.ValidationError;
    }
}
