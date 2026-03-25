// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.EventStores;

/// <summary>
/// Lists all event stores.
/// </summary>
[CliCommand("list", "List all event stores", Branch = typeof(ChronicleBranch.EventStores))]
[CliExample("chronicle", "event-stores", "list")]
[LlmOutputAdvice("plain", "plain is ~3x smaller (29B vs 99B). JSON wraps each name in {\"value\": ...}.")]
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
