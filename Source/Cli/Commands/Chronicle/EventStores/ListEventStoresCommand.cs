// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.EventStores;

/// <summary>
/// Lists all event stores.
/// </summary>
public class ListEventStoresCommand : ChronicleCommand<ChronicleSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ChronicleSettings settings, string format)
    {
        var eventStores = await services.EventStores.GetEventStores();
        var names = eventStores.ToList();

        OutputFormatter.Write(
            format,
            names,
            ["Name"],
            name => [name]);

        return ExitCodes.Success;
    }
}
