// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using Grpc.Core;

namespace Cratis.Cli.Commands.Chronicle.Streams;

/// <summary>
/// Lists closed stream scopes and their closure metadata.
/// </summary>
[LlmDescription("Lists closed stream scopes within optional event source and stream dimensions. Returns the dimensions, origin (CompleteStream or ClosingEvent), owning constraint (closedBy), sequence number and closing time. This is not paged: narrow large sequences with scope filters. Requires Chronicle 19.39.0 or later.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("list", "List closed stream scopes", Branch = typeof(ChronicleBranch.Streams.Closed))]
[CliExample("chronicle", "streams", "closed", "list")]
[CliExample("chronicle", "streams", "closed", "list", "--event-source-id", "order-42", "-o", "json")]
[LlmOption("--sequence", "string", "Event sequence name (default: event-log)")]
[LlmOption("--event-source-id", "string", "Filter by event source identifier")]
[LlmOption("--event-source-type", "string", "Filter by event source type")]
[LlmOption("--event-stream-type", "string", "Filter by event stream type")]
[LlmOption("--event-stream-id", "string", "Filter by event stream identifier")]
public class ListClosedStreamsCommand : ChronicleCommand<StreamScopeSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, StreamScopeSettings settings, string format)
    {
        QueryResult<IEnumerable<ClosedStreamResponse>> result;
        try
        {
            result = await services.Sequences.ClosedStreams(new ClosedStreamsRequest
            {
                EventStore = settings.ResolveEventStore(),
                Namespace = settings.ResolveNamespace(),
                EventSequenceId = settings.EventSequenceId,
                EventSourceId = settings.EventSourceId,
                EventSourceType = settings.EventSourceType,
                EventStreamType = settings.EventStreamType,
                EventStreamId = settings.EventStreamId
            });
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Unimplemented)
        {
            OutputFormatter.WriteError(format, "Listing closed stream scopes requires Chronicle 19.39.0 or later.", "Upgrade the Chronicle server to 19.39.0 or later.", ExitCodes.ValidationErrorCode);
            return ExitCodes.ValidationError;
        }

        if (!result.IsSuccess)
        {
            var message = !result.IsAuthorized
                ? "The server did not authorize listing closed stream scopes."
                : string.Join("; ", result.ExceptionMessages.Concat(result.ValidationResults.Select(_ => _.Message)));
            OutputFormatter.WriteError(format, message, "Check your credentials and event sequence scope.", ExitCodes.ServerErrorCode);
            return ExitCodes.ServerError;
        }

        var closures = result.Data.Select(closure => new
        {
            closure.EventSourceId,
            closure.EventSourceType,
            closure.EventStreamType,
            closure.EventStreamId,
            Origin = closure.Origin.ToString(),
            closure.ClosedBy,
            closure.SequenceNumber,
            ClosedAt = (DateTimeOffset?)closure.ClosedAt
        });

        OutputFormatter.Write(
            format,
            closures,
            ["EventSourceId", "EventSourceType", "EventStreamType", "EventStreamId", "Origin", "ClosedBy", "SequenceNumber", "ClosedAt"],
            closure =>
            [
                closure.EventSourceId ?? string.Empty,
                closure.EventSourceType ?? string.Empty,
                closure.EventStreamType ?? string.Empty,
                closure.EventStreamId ?? string.Empty,
                closure.Origin,
                closure.ClosedBy ?? string.Empty,
                closure.SequenceNumber.ToString(),
                closure.ClosedAt?.ToString("O") ?? string.Empty
            ]);

        return ExitCodes.Success;
    }
}
