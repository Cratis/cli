// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.ReadModels;

/// <summary>
/// Lists read model definitions in an event store.
/// </summary>
public class ListReadModelsCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, EventStoreSettings settings, string format)
    {
        var response = await services.ReadModels.GetDefinitions(new GetDefinitionsRequest
        {
            EventStore = settings.ResolveEventStore()
        });

        if (format is OutputFormats.Json or OutputFormats.JsonCompact)
        {
            var dtos = response.ReadModels.Select(rm => new
            {
                identifier = rm.Type?.Identifier ?? string.Empty,
                generation = rm.Type?.Generation ?? 0,
                containerName = rm.ContainerName,
                displayName = rm.DisplayName,
                observerType = rm.ObserverType.ToString(),
                observerIdentifier = rm.ObserverIdentifier,
                owner = rm.Owner.ToString(),
                source = rm.Source.ToString()
            });
            OutputFormatter.WriteObject(format, dtos);
        }
        else
        {
            OutputFormatter.Write(
                format,
                response.ReadModels,
                ["Identifier", "Container", "DisplayName", "ObserverType", "Owner", "Source"],
                rm =>
                [
                    rm.Type?.Identifier ?? string.Empty,
                    rm.ContainerName,
                    rm.DisplayName,
                    rm.ObserverType.ToString(),
                    rm.Owner.ToString(),
                    rm.Source.ToString()
                ]);
        }

        return ExitCodes.Success;
    }
}
