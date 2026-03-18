// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Detects first-run scenarios and shows a getting-started hint when no configuration exists.
/// </summary>
public static class FirstRunDetector
{
    /// <summary>
    /// Writes a getting-started hint when no configuration file is found.
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

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]No configuration found. Get started:[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Accent.ToMarkup()}]1.[/] Create a context pointing at your server:");
        AnsiConsole.MarkupLine("     [bold]cratis context create dev --server chronicle://localhost:35000/?disableTls=true[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Accent.ToMarkup()}]2.[/] Then query your system:");
        AnsiConsole.MarkupLine("     [bold]cratis chronicle observers list[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]Run [bold]cratis --help[/] to see all commands.[/]");
        AnsiConsole.WriteLine();
    }
}
