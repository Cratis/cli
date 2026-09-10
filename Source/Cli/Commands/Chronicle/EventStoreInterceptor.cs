// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Interceptor that ensures a valid default event store is configured before any
/// <see cref="EventStoreSettings"/> command runs.
/// Triggers when no event store is set, or when the stored event store no longer exists on the server.
/// Only active in interactive terminals.
/// </summary>
public class EventStoreInterceptor : ICommandInterceptor
{
    /// <inheritdoc/>
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is not EventStoreSettings eventStoreSettings)
        {
            return;
        }

        // If the user passed --event-store explicitly, skip prompting.
        if (!string.IsNullOrWhiteSpace(eventStoreSettings.EventStore))
        {
            return;
        }

        // Skip prompting when --yes is set or a person cannot safely answer the prompt.
        if (settings is GlobalSettings { Yes: true } || !GlobalSettings.IsInteractiveEnvironment())
        {
            return;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        var connectionString = new ChronicleConnectionString(eventStoreSettings.ResolveConnectionString());

        // Pass the currently stored event store so the selector can validate it is still present.
        // If it is missing or empty the selector will prompt the user and save the selection.
        EventStoreSelector.TryPromptAndSave(connectionString, config, ctx, ctx.EventStore);
    }
}
