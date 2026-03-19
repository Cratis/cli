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

        var accent = OutputFormatter.Accent.ToMarkup();
        var muted = OutputFormatter.Muted.ToMarkup();

        var content = new Markup(
            $"  [{accent}]1.[/] Create a context pointing at your server:\n" +
            "     [bold]cratis context create dev \\\n" +
            "       --server chronicle://localhost:35000/?disableTls=true[/]\n\n" +
            $"  [{accent}]2.[/] Then query your system:\n" +
            "     [bold]cratis chronicle observers list[/]");

        var panel = new Panel(content)
            .Header($"[{accent}] Getting Started [/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(OutputFormatter.Accent))
            .Padding(1, 1);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{muted}]Run [bold]cratis --help[/] to see all commands.[/]");
        AnsiConsole.WriteLine();
    }
}
