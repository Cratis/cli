// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

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

        if (AnsiConsole.Profile.Width >= 115)
        {
            GradientFigletRenderer.RenderLines(_logo, OutputFormatter.BannerGradient);
        }
        else
        {
            GradientFigletRenderer.Render("cratis", OutputFormatter.BannerGradient);
        }

        RenderGradientRule();

        var version = typeof(CliApp).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]v{version} \u2014 The Cratis Platform CLI[/]");
        AnsiConsole.WriteLine();
    }

    static void RenderGradientRule()
    {
        var width = Math.Min(AnsiConsole.Profile.Width - 4, 50);
        var sb = new StringBuilder();

        for (var i = 0; i < width; i++)
        {
            var t = width > 1 ? (float)i / (width - 1) : 0.5f;
            var color = GradientFigletRenderer.Interpolate(OutputFormatter.BannerGradient, t);
            sb.Append($"[{color.ToMarkup()}]\u2500[/]");
        }

        AnsiConsole.MarkupLine($"  {sb}");
    }
}
