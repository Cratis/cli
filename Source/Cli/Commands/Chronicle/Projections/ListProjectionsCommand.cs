// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Projections;

/// <summary>
/// Lists projection definitions in an event store.
/// </summary>
[LlmDescription("Lists all projection definitions registered in the namespace. Use -o plain to suppress verbose JSON schema fields. Use to audit what projections are registered.")]
[CliCommand("list", "List projection definitions", Branch = typeof(ChronicleBranch.Projections))]
[CliExample("chronicle", "projections", "list")]
[LlmOutputAdvice("plain", "JSON output is very large due to full projection definitions. Plain shows the key fields concisely.")]
public class ListProjectionsCommand : ChronicleCommand<EventStoreSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, EventStoreSettings settings, string format)
    {
        var definitions = await services.Projections.GetAllDefinitions(new GetAllDefinitionsRequest
        {
            EventStore = settings.ResolveEventStore()
        });

        var list = definitions.ToList();

        OutputFormatter.Write(
            format,
            list,
            ["Identifier", "ReadModel", "Active", "Rewindable", "AutoMap"],
            def =>
            [
                def.Identifier,
                def.ReadModel,
                def.IsActive.ToString(),
                def.IsRewindable.ToString(),
                def.AutoMap.ToString()
            ]);

        return ExitCodes.Success;
    }
}
