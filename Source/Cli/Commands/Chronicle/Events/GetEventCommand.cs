// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Events;

/// <summary>
/// Gets a specific event by its sequence number.
/// </summary>
public class GetEventCommand : ChronicleCommand<GetEventSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, GetEventSettings settings, string format)
    {
        var response = await services.EventSequences.GetEventsFromEventSequenceNumber(new GetFromEventSequenceNumberRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            EventSequenceId = settings.EventSequenceId,
            FromEventSequenceNumber = settings.SequenceNumber,
            ToEventSequenceNumber = settings.SequenceNumber
        });

        var evt = response.Events?.FirstOrDefault();

        if (evt is null)
        {
            OutputFormatter.WriteError(
                format,
                $"No event found at sequence number {settings.SequenceNumber}",
                "Use 'cratis chronicle events get' to browse events",
                ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        if (format is OutputFormats.Json or OutputFormats.JsonCompact)
        {
            var ctx = evt.Context;
            JsonElement? content = null;
            if (!string.IsNullOrEmpty(evt.Content))
            {
                try { content = JsonSerializer.Deserialize<JsonElement>(evt.Content); }
                catch { content = null; }
            }

            OutputFormatter.WriteObject(format, new
            {
                sequenceNumber = ctx.SequenceNumber,
                eventType = ctx.EventType?.Id,
                generation = ctx.EventType?.Generation,
                eventSourceId = ctx.EventSourceId,
                occurred = ctx.Occurred,
                correlationId = ctx.CorrelationId,
                causedBy = ctx.CausedBy?.Subject,
                content
            });
        }
        else
        {
            var ctx = evt.Context;
            OutputFormatter.Write(
                format,
                [evt],
                ["Seq#", "EventType", "EventSourceId", "Occurred"],
                e =>
                [
                    e.Context.SequenceNumber.ToString(),
                    e.Context.EventType?.Id ?? string.Empty,
                    e.Context.EventSourceId ?? string.Empty,
                    e.Context.Occurred?.ToString() ?? string.Empty
                ]);

            if (!string.IsNullOrEmpty(evt.Content))
            {
                AnsiConsole.WriteLine();
                OutputFormatter.WriteSection("Content");
                AnsiConsole.WriteLine(evt.Content);
            }
        }

        return ExitCodes.Success;
    }
}
