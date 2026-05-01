// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Events;

/// <summary>
/// Gets events from an event sequence.
/// </summary>
[CliCommand("get", "Get events from an event sequence", Branch = typeof(ChronicleBranch.Events))]
[CliExample("chronicle", "events", "get", "-o", "plain")]
[CliExample("chronicle", "events", "get", "--from", "100", "--to", "200")]
[CliExample("chronicle", "events", "get", "--event-type", "UserRegistered")]
[LlmOutputAdvice("plain", "plain is ~25x smaller (6.8KB vs 169KB for 73 events). JSON includes context, causation chains, content.")]
public class GetEventsCommand : ChronicleCommand<GetEventsSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, GetEventsSettings settings, string format)
    {
        var request = new GetFromEventSequenceNumberRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            EventSequenceId = settings.EventSequenceId,
            FromEventSequenceNumber = settings.From,
            ToEventSequenceNumber = settings.To,
            EventSourceId = settings.EventSourceId
        };

        if (!string.IsNullOrEmpty(settings.EventType))
        {
            foreach (var parsed in EventTypeParser.ParseEventTypes(settings.EventType))
            {
                request.EventTypes.Add(parsed);
            }
        }

        var response = await services.EventSequences.GetEventsFromEventSequenceNumber(request);

        if (string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) || string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal))
        {
            var dtos = response.Events.Select(evt =>
            {
                var ctx = evt.Context;
                JsonElement? content = null;
                if (!string.IsNullOrEmpty(evt.Content))
                {
                    try { content = JsonSerializer.Deserialize<JsonElement>(evt.Content); }
                    catch { content = null; }
                }

                return new
                {
                    sequenceNumber = ctx.SequenceNumber,
                    eventType = ctx.EventType?.Id,
                    generation = ctx.EventType?.Generation,
                    eventSourceId = ctx.EventSourceId,
                    occurred = ctx.Occurred,
                    correlationId = ctx.CorrelationId,
                    causedBy = ctx.CausedBy?.Subject,
                    content
                };
            });
            OutputFormatter.WriteObject(format, dtos);
        }
        else
        {
            OutputFormatter.Write(
                format,
                response.Events,
                ["Seq#", "EventType", "EventSourceId", "Occurred"],
                evt =>
                [
                    evt.Context.SequenceNumber.ToString(),
                    evt.Context.EventType.Id,
                    evt.Context.EventSourceId,
                    evt.Context.Occurred?.ToString() ?? string.Empty
                ]);
        }

        return ExitCodes.Success;
    }
}
