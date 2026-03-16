// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console;

namespace Cratis.Cli;

/// <summary>
/// Renders the Cratis CLI banner.
/// </summary>
public static class Banner
{
    /// <summary>
    /// Renders the branded banner to the console.
    /// </summary>
    public static void Render()
    {
        AnsiConsole.WriteLine();

        var logo = new FigletText("cratis")
            .Color(OutputFormatter.Accent);

        AnsiConsole.Write(logo);

        var version = typeof(CliApp).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]v{version} \u2014 The Cratis Platform CLI[/]");
        AnsiConsole.WriteLine();
    }
}
