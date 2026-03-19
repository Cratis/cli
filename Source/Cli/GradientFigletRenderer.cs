// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli;

/// <summary>
/// Renders text with a horizontal color gradient applied per-column.
/// </summary>
public static class GradientFigletRenderer
{
    /// <summary>
    /// Renders an array of pre-formed ASCII art lines with a multi-stop horizontal gradient.
    /// </summary>
    /// <param name="lines">The lines to render.</param>
    /// <param name="gradientStops">The gradient color stops (left to right).</param>
    public static void RenderLines(string[] lines, Color[] gradientStops)
    {
        var maxWidth = lines.Max(l => l.Length);

        foreach (var line in lines)
        {
            var sb = new StringBuilder();
            for (var col = 0; col < line.Length; col++)
            {
                var ch = line[col];
                if (ch == ' ')
                {
                    sb.Append(' ');
                    continue;
                }

                var t = maxWidth > 1 ? (float)col / (maxWidth - 1) : 0.5f;
                var color = Interpolate(gradientStops, t);
                sb.Append($"[{color.ToMarkup()}]{ch.ToString().EscapeMarkup()}[/]");
            }

            AnsiConsole.MarkupLine(sb.ToString());
        }
    }

    /// <summary>
    /// Renders figlet text with a multi-stop horizontal color gradient.
    /// </summary>
    /// <param name="text">The text to render in figlet font.</param>
    /// <param name="gradientStops">The gradient color stops (left to right).</param>
    public static void Render(string text, Color[] gradientStops)
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Out = new AnsiConsoleOutput(writer),
            ColorSystem = ColorSystemSupport.NoColors
        });

        console.Write(new FigletText(text));

        var lines = writer.ToString()
            .Split('\n')
            .Select(l => l.TrimEnd())
            .Where(l => l.Length > 0)
            .ToArray();

        RenderLines(lines, gradientStops);
    }

    /// <summary>
    /// Interpolates between multiple color stops at position <paramref name="t"/> (0.0 to 1.0).
    /// </summary>
    /// <param name="stops">Array of color stops distributed evenly from 0 to 1.</param>
    /// <param name="t">Position in the gradient (0.0 = first stop, 1.0 = last stop).</param>
    /// <returns>The interpolated color.</returns>
    public static Color Interpolate(Color[] stops, float t)
    {
        if (stops.Length == 1)
        {
            return stops[0];
        }

        t = Math.Clamp(t, 0f, 1f);
        var segmentCount = stops.Length - 1;
        var scaled = t * segmentCount;
        var index = (int)Math.Floor(scaled);

        if (index >= segmentCount)
        {
            return stops[^1];
        }

        var local = scaled - index;
        var from = stops[index];
        var to = stops[index + 1];

        return new Color(
            (byte)(from.R + ((to.R - from.R) * local)),
            (byte)(from.G + ((to.G - from.G) * local)),
            (byte)(from.B + ((to.B - from.B) * local)));
    }
}
