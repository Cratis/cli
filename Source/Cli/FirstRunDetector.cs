// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Handles the first-run experience when no configuration file exists.
/// Creates a default context pointing at localhost and prints a welcome message.
/// Event store selection happens automatically on the first chronicle command via <see cref="Commands.Chronicle.EventStoreInterceptor"/>.
/// </summary>
public static class FirstRunDetector
{
    const string DefaultServer = "chronicle://localhost:35000/?disableTls=true";

    /// <summary>
    /// Bootstraps a default context when no configuration file is found and prints a welcome message.
    /// Does nothing when output is redirected or a config file already exists.
    /// </summary>
    public static void ShowIfNeeded()
    {
        if (Console.IsOutputRedirected)
        {
            return;
        }

        var configPath = CliConfiguration.GetConfigPath();
        if (File.Exists(configPath))
        {
            return;
        }

        var accent = OutputFormatter.Accent.ToMarkup();
        var muted = OutputFormatter.Muted.ToMarkup();

        // Create and persist the default context so all subsequent commands resolve the connection
        // string without any manual setup.
        var config = new CliConfiguration
        {
            ActiveContext = CliConfiguration.DefaultContextName
        };
        var ctx = config.GetCurrentContext();
        ctx.Server = DefaultServer;
        config.Save();

        AnsiConsole.MarkupLine($"[{accent}]Welcome to Cratis CLI![/]");
        AnsiConsole.MarkupLine($"  [{muted}]Created default context →[/] [bold]{DefaultServer}[/]");
        AnsiConsole.MarkupLine($"  [{muted}]Run any[/] [bold]cratis chronicle[/] [{muted}]command and you will be prompted to choose a default event store.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{muted}]Run [bold]cratis --help[/] to see all commands.[/]");
        AnsiConsole.WriteLine();
    }
}

