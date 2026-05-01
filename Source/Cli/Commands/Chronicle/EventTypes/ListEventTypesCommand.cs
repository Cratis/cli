// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.EventTypes;

/// <summary>
/// Lists registered event types in an event store.
/// </summary>
[CliCommand("list", "List registered event types", Branch = typeof(ChronicleBranch.EventTypes))]
[CliExample("chronicle", "event-types", "list")]
[CliExample("chronicle", "event-types", "list", "-e", "MyStore", "-o", "plain")]
[LlmOutputAdvice("plain", "plain is ~34x smaller (1.2KB vs 41KB). JSON includes full JSON Schema blob per event type.")]
public class ListEventTypesCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, EventStoreSettings settings, string format)
    {
        var registrations = await services.EventTypes.GetAllRegistrations(new GetAllEventTypesRequest { EventStore = settings.ResolveEventStore() });
        var list = registrations.ToList();

        if (string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) || string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal))
        {
            var dtos = list.Select(reg => new
            {
                type = new
                {
                    id = reg.Type.Id,
                    generation = reg.Type.Generation,
                    tombstone = reg.Type.Tombstone
                },
                owner = reg.Owner.ToString(),
                source = reg.Source.ToString()
            });
            OutputFormatter.WriteObject(format, dtos);
        }
        else
        {
            OutputFormatter.Write(
                format,
                list,
                ["Id", "Generation", "Owner", "Source"],
                reg => [reg.Type.Id, reg.Type.Generation.ToString(), reg.Owner.ToString(), reg.Source.ToString()]);
        }

        return ExitCodes.Success;
    }
}
