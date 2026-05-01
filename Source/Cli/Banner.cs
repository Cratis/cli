// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli;

/// <summary>
/// Renders the Cratis CLI banner with a gradient logo.
/// </summary>
public static class Banner
{
    static readonly string[] _logo =
    [
        "                           @@@@@",
        "                        @@@@@@@@",
        "                    @@@@@@@@@@@@",
        "                 @@@@@@@@@@@@@@@",
        "             @@@@@@@@@@@@@@@@@@@",
        "          @@@@@@@@@@@@@@@@@@@@@@",
        "       @@@@@@@@@@@@@@@@@@@@@@@",
        "      @@@@@@@@@@@@@@@@@@@@@                                                                   @@@",
        "      @@@@@@@@@@@@@@@@@                                                                      @@@@@",
        "      @@@@@@@@@@@@@@                                                                 @@@@      @@",
        "      @@@@@@@@@@@@           @@@@@@@               @@@@@@@   @@@@  @@@  @@@@@@@    @@@@@@@@@  @@@@    @@@@@@@",
        "      @@@@@@@@@@@@       @@@@@@@@@@@@@@          @@@@@@@@@@@ @@@@@@@@@ @@@@@@@@@@@ @@@@@@@@@  @@@@  @@@@@@@@@@",
        "      @@@@@@@@@@@@       @@@@@@@@@@@@@@@       @@@@@     @@  @@@@@            @@@@   @@@@     @@@@  @@@",
        "      @@@@@@@@@@@@       @@@@@@@@@@@@@@@       @@@@          @@@@      @@@@@@@@@@@   @@@@     @@@@  @@@@@@@@@@",
        "      @@@@@@@@@@@@       @@@@@@@@@@@@@@@       @@@@@     @   @@@@     @@@@    @@@@   @@@@     @@@@         @@@@",
        "      @@@@@@@@@@@@       @@@@@@@@@@@@@@@        @@@@@@@@@@@@ @@@@     @@@@@  @@@@@   @@@@@@@@ @@@@  @@@@@@@@@@@",
        "      @@@@@@@@@@@@        @@@@@@@@@@@@@           @@@@@@@@   @@@@      @@@@@@@ @@@     @@@@@@ @@@@  @@@@@@@@@",
        "      @@@@@@@@@@@@           @@@@@@@",
        "      @@@@@@@@@@@@@@",
        "      @@@@@@@@@@@@@@@@@@",
        "      @@@@@@@@@@@@@@@@@@@@@",
        "       @@@@@@@@@@@@@@@@@@@@@@@",
        "           @@@@@@@@@@@@@@@@@@@@@",
        "              @@@@@@@@@@@@@@@@@@",
        "                 @@@@@@@@@@@@@@@",
        "                     @@@@@@@@@@@",
        "                         @@@@@@@",
        "                            @@@@",
    ];

    /// <summary>
    /// Renders the branded banner to the console.
    /// </summary>
    public static void Render()
    {
        AnsiConsole.WriteLine();

        var logoWidth = _logo.Max(l => l.Length);
        var targetWidth = Math.Min(AnsiConsole.Profile.Width - 2, logoWidth);
        GradientFigletRenderer.RenderLines(_logo, OutputFormatter.BannerGradient, targetWidth);

        var version = typeof(CliApp).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]v{version} \u2014 The Cratis Platform CLI[/]");
        AnsiConsole.WriteLine();
    }
}
