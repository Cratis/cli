// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

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
        if (settings is GlobalSettings { Yes: true } || !IsInteractive())
        {
            return;
        }

        CliConfiguration config;
        CliContext ctx;
        ChronicleConnectionString connectionString;
        try
        {
            config = CliConfiguration.Load();
            ctx = config.GetCurrentContext();
            connectionString = new ChronicleConnectionString(eventStoreSettings.ResolveConnectionString());
        }
        catch (Exception ex) when (ex is InvalidServerAddress or MissingServerAddress or FormatException or ArgumentException or JsonException)
        {
            // Interceptors run before the command's error handler. Report once in the selected format.
            ChronicleCommand<EventStoreSettings>.ReportConnectionResolutionError(eventStoreSettings.ResolveOutputFormat(), ex);
            eventStoreSettings.ConnectionResolutionReported = true;
            return;
        }
        catch (LoginSessionExpired ex)
        {
            // Interceptors run before the command's own error handler. Do not throw an
            // unhandled exception; the command will return the authentication exit code.
            OutputFormatter.WriteError(eventStoreSettings.ResolveOutputFormat(), "Login expired", ex.Message, ExitCodes.AuthenticationErrorCode);
            eventStoreSettings.LoginExpiredReported = true;
            return;
        }

        // Pass the currently stored event store so the selector can validate it is still present.
        // If it is missing or empty the selector will prompt the user and save the selection.
        EventStoreSelector.TryPromptAndSave(connectionString, config, ctx, ctx.EventStore);
    }

    /// <summary>
    /// Determines whether an interactive prompt is available.
    /// </summary>
    /// <returns>Whether the terminal is interactive.</returns>
    protected virtual bool IsInteractive() => GlobalSettings.IsInteractiveEnvironment();
}
