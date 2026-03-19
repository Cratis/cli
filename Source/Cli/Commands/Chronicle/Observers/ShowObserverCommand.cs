// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Observers;

/// <summary>
/// Shows detailed information about a specific observer.
/// </summary>
public class ShowObserverCommand : ChronicleCommand<ObserverCommandSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteCommandAsync(IServices services, ObserverCommandSettings settings, string format)
    {
        var info = await services.Observers.GetObserverInformation(new GetObserverInformationRequest
        {
            EventStore = settings.ResolveEventStore(),
            Namespace = settings.ResolveNamespace(),
            ObserverId = settings.ObserverId,
            EventSequenceId = settings.EventSequenceId
        });

        var eventTypes = (info.EventTypes ?? []).Select(et => $"{et.Id}+{et.Generation}").ToList();

        var lastHandled = info.LastHandledEventSequenceNumber == ulong.MaxValue ? null : (ulong?)info.LastHandledEventSequenceNumber;

        OutputFormatter.WriteObject(
            format,
            new
            {
                id = info.Id,
                eventSequenceId = info.EventSequenceId,
                type = info.Type.ToString(),
                owner = info.Owner.ToString(),
                runningState = info.RunningState.ToString(),
                nextEventSequenceNumber = info.NextEventSequenceNumber,
                lastHandledEventSequenceNumber = lastHandled,
                isSubscribed = info.IsSubscribed,
                eventTypes
            },
            data =>
            {
                AnsiConsole.MarkupLine($"[bold]Observer:[/]     {data.id.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Sequence:[/]     {data.eventSequenceId.EscapeMarkup()}");
                AnsiConsole.MarkupLine($"[bold]Type:[/]         {data.type}");
                AnsiConsole.MarkupLine($"[bold]Owner:[/]        {data.owner}");
                AnsiConsole.MarkupLine($"[bold]State:[/]        {data.runningState}");
                AnsiConsole.MarkupLine($"[bold]Next#:[/]        {data.nextEventSequenceNumber}");
                AnsiConsole.MarkupLine($"[bold]LastHandled#:[/] {(lastHandled.HasValue ? lastHandled.Value.ToString() : "(never)")}");
                AnsiConsole.MarkupLine($"[bold]Subscribed:[/]   {data.isSubscribed}");
                AnsiConsole.MarkupLine($"[bold]EventTypes:[/]   {string.Join(", ", data.eventTypes)}");
            });

        return ExitCodes.Success;
    }
}
