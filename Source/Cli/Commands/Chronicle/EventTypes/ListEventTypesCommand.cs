// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.EventTypes;

/// <summary>
/// Lists registered event types in an event store.
/// </summary>
public class ListEventTypesCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, EventStoreSettings settings, string format)
    {
        var registrations = await services.EventTypes.GetAllRegistrations(new GetAllEventTypesRequest { EventStore = settings.ResolveEventStore() });
        var list = registrations.ToList();

        if (format is OutputFormats.Json or OutputFormats.JsonCompact)
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
