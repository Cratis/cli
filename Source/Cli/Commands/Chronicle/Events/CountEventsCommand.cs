// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Events;

/// <summary>
/// Gets the tail (highest) sequence number in an event sequence. This is not a count of events — gaps may exist.
/// </summary>
public class CountEventsCommand : ChronicleCommand<CountEventsSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, CountEventsSettings settings, string format)
    {
        var request = new GetTailSequenceNumberRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            EventSequenceId = settings.EventSequenceId
        };

        if (!string.IsNullOrWhiteSpace(settings.EventType))
        {
            request.EventTypes = EventTypeParser.ParseEventTypes(settings.EventType);
        }

        if (!string.IsNullOrWhiteSpace(settings.EventSourceId))
        {
            request.EventSourceId = settings.EventSourceId;
        }

        var response = await services.EventSequences.GetTailSequenceNumber(request);

        if (format is OutputFormats.Json or OutputFormats.JsonCompact)
        {
            OutputFormatter.WriteObject(format, new { tailSequenceNumber = response.SequenceNumber });
        }
        else
        {
            OutputFormatter.Write(
                format,
                [response.SequenceNumber],
                ["TailSequenceNumber"],
                n => [n.ToString()]);
        }

        return ExitCodes.Success;
    }
}
