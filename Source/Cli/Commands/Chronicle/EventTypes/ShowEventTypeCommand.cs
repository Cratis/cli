// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.Events;

namespace Cratis.Cli.Commands.Chronicle.EventTypes;

/// <summary>
/// Shows a specific event type registration including its JSON schema.
/// </summary>
[CliCommand("show", "Show an event type registration with its JSON schema", Branch = typeof(ChronicleBranch.EventTypes))]
[CliExample("chronicle", "event-types", "show", "UserRegistered")]
[CliExample("chronicle", "event-types", "show", "UserRegistered+1", "-o", "json")]
[LlmOutputAdvice("json", "JSON contains the full JSON schema. Use JSON to parse schema fields.")]
[LlmOption("<EVENT_TYPE>", "string", "Event type identifier: name or name+generation (positional)")]
public class ShowEventTypeCommand : ChronicleCommand<ShowEventTypeSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ShowEventTypeSettings settings, string format)
    {
        var parsed = EventTypeParser.ParseEventType(settings.EventType);

        var registrations = await services.EventTypes.GetAllRegistrations(new GetAllEventTypesRequest
        {
            EventStore = settings.ResolveEventStore()
        });

        var match = registrations.FirstOrDefault(r =>
            string.Equals(r.Type.Id, parsed.Id, StringComparison.OrdinalIgnoreCase) &&
            r.Type.Generation == parsed.Generation);

        if (match is null)
        {
            OutputFormatter.WriteError(
                format,
                $"Event type '{settings.EventType}' not found",
                "Use 'cratis event-types list' to see registered event types",
                ExitCodes.NotFoundCode);
            return ExitCodes.NotFound;
        }

        OutputFormatter.WriteObject(
            format,
            new
            {
                match.Type.Id,
                match.Type.Generation,
                match.Type.Tombstone,
                Owner = match.Owner.ToString(),
                Source = match.Source.ToString(),
                match.Schema
            },
            data =>
            {
                AnsiConsole.MarkupLine($"[bold]EventType:[/]  {data.Id.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Generation:[/] {data.Generation}");
                AnsiConsole.MarkupLine($"[bold]Tombstone:[/]  {data.Tombstone}");
                AnsiConsole.MarkupLine($"[bold]Owner:[/]      {data.Owner}");
                AnsiConsole.MarkupLine($"[bold]Source:[/]     {data.Source}");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Schema:[/]");
                AnsiConsole.WriteLine(data.Schema);
            });

        return ExitCodes.Success;
    }
}
