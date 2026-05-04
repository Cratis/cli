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

        // If the user passed --event-store explicitly (not the default), skip prompting.
        if (eventStoreSettings.EventStore != CliDefaults.DefaultEventStoreName)
        {
            return;
        }

        // Skip prompting when --yes flag is set or in non-interactive terminals.
        if (settings is GlobalSettings { Yes: true })
        {
            return;
        }

        if (!AnsiConsole.Profile.Out.IsTerminal)
        {
            return;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        var connectionString = new ChronicleConnectionString(eventStoreSettings.ResolveConnectionString());
        var managementPort = eventStoreSettings.ResolveManagementPort();

        // Pass the currently stored event store so the selector can validate it is still present.
        // If it is missing or empty the selector will prompt the user and save the selection.
        EventStoreSelector.TryPromptAndSave(connectionString, managementPort, config, ctx, ctx.EventStore);
    }
}
