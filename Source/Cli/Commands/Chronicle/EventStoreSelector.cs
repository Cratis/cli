// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Outcome of a <see cref="EventStoreSelector.TryPromptAndSave"/> call.
/// </summary>
public enum EventStoreSelectorResult
{
    /// <summary>The Chronicle server could not be reached.</summary>
    Unreachable,

    /// <summary>Connected, but the server returned no event stores.</summary>
    NoEventStores,

    /// <summary>The existing event store was validated and is still present — no prompt shown.</summary>
    AlreadyValid,

    /// <summary>The user selected an event store and it was saved to the context.</summary>
    Selected,

    /// <summary>The user was prompted but declined to select (e.g. cancelled the single-store confirm).</summary>
    Declined,
}

/// <summary>
/// Shared helper for fetching event stores from the server and prompting the user to choose a default.
/// Used by <see cref="EventStoreInterceptor"/> and <see cref="FirstRunDetector"/>.
/// </summary>
public static class EventStoreSelector
{
    /// <summary>
    /// Connects to the Chronicle server, fetches available event stores, validates or prompts for a
    /// default selection, and saves the choice to the context in the provided <see cref="CliConfiguration"/>.
    /// </summary>
    /// <param name="connectionString">The Chronicle connection string to use.</param>
    /// <param name="managementPort">The management port.</param>
    /// <param name="config">The CLI configuration to update and save.</param>
    /// <param name="ctx">The current context to update.</param>
    /// <param name="currentEventStore">
    /// The event store name already stored in the context, if any.
    /// When non-empty, the list is fetched to validate it is still present on the server.
    /// When the stored name is not found, the user is re-prompted.
    /// </param>
    /// <returns>An <see cref="EventStoreSelectorResult"/> describing the outcome.</returns>
    public static EventStoreSelectorResult TryPromptAndSave(
        ChronicleConnectionString connectionString,
        int managementPort,
        CliConfiguration config,
        CliContext ctx,
        string? currentEventStore = null)
    {
        // Use Task.Run to execute the async connection and gRPC call in a clean thread-pool context.
        // Calling ConnectSync (blocking) followed by GetEventStores().GetAwaiter().GetResult() from a
        // thread that itself was resumed from a blocked GetResult() call can cause deadlocks with the
        // gRPC channel's internal completion machinery. Task.Run gives us a fresh context with no
        // blocked thread in the chain.
        List<string> eventStores;
        try
        {
            eventStores = Task.Run(async () =>
            {
                using var client = await CliChronicleConnection.Connect(connectionString, managementPort);
                return (await client.Services.EventStores.GetEventStores()).ToList();
            }).GetAwaiter().GetResult();
        }
        catch
        {
            // Server unreachable — caller decides how to report this.
            return EventStoreSelectorResult.Unreachable;
        }

        if (eventStores.Count == 0)
        {
            return EventStoreSelectorResult.NoEventStores;
        }

        // Validate the currently stored event store — if it exists, nothing to do.
        if (!string.IsNullOrWhiteSpace(currentEventStore) && eventStores.Contains(currentEventStore, StringComparer.Ordinal))
        {
            return EventStoreSelectorResult.AlreadyValid;
        }

        // Tell the user why we are prompting.
        if (!string.IsNullOrWhiteSpace(currentEventStore))
        {
            AnsiConsole.MarkupLine($"[yellow]Event store '[bold]{currentEventStore.EscapeMarkup()}[/]' no longer exists on this server.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No default event store configured.[/]");
        }

        string selected;

        if (eventStores.Count == 1)
        {
            selected = eventStores[0];
            if (!AnsiConsole.Confirm($"Use [green]{selected.EscapeMarkup()}[/] as default event store?"))
            {
                return EventStoreSelectorResult.Declined;
            }
        }
        else
        {
            selected = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select a default event store:")
                    .AddChoices(eventStores));
        }

        ctx.EventStore = selected;
        config.Save();

        AnsiConsole.MarkupLine($"[green]Default event store set to '[bold]{selected.EscapeMarkup()}[/]'.[/]");
        AnsiConsole.MarkupLine("[dim]You can change it later with: cratis context set-value event-store <name>[/]");
        AnsiConsole.WriteLine();
        return EventStoreSelectorResult.Selected;
    }
}
