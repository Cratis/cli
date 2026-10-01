// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console;
using Spectre.Console.Rendering;

namespace Cratis.Cli.for_DiagnoseCommand.given;

public class captured_reports : Specification
{
    protected DiagnoseData _data;
    protected Table _watchReport;
    protected bool _watchPending;
    protected readonly Dictionary<string, string> _outputs = new(StringComparer.Ordinal);

    void Establish() => _data = new DiagnoseData("chronicle://localhost:35000", "store", "tenant-one", true, null, null, ["store"], 0, 0, 0, 0, 0, 0, 10, DateTimeOffset.UtcNow);

    protected IEnumerable<Segment> WatchIcons(string label) => _watchReport.Rows
        .Where(row => string.Concat(row[1].GetSegments(AnsiConsole.Console).Select(segment => segment.Text)) == label)
        .SelectMany(row => row[0].GetSegments(AnsiConsole.Console));

    protected void CaptureReports()
    {
        foreach (var format in new[] { OutputFormats.Json, OutputFormats.JsonCompact, OutputFormats.JsonQuiet, OutputFormats.Plain, OutputFormats.Table, OutputFormats.Auto, OutputFormats.Quiet, "watch" })
        {
            using var writer = new StringWriter();
            var previousOutput = Console.Out;
            var previousConsole = AnsiConsole.Console;
            try
            {
                Console.SetOut(writer);
                AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings
                {
                    Out = new AnsiConsoleOutput(writer),
                    Ansi = AnsiSupport.No,
                    ColorSystem = ColorSystemSupport.NoColors
                });
                AnsiConsole.Console.Profile.Width = 240;
                if (format == "watch")
                {
                    _watchReport = DiagnoseCommand.BuildWatchReport(_data, pending: _watchPending);
                    AnsiConsole.Write(_watchReport);
                }
                else
                {
                    DiagnoseCommand.Render(format, _data);
                }

                _outputs[format] = writer.ToString();
            }
            finally
            {
                Console.SetOut(previousOutput);
                AnsiConsole.Console = previousConsole;
            }
        }
    }
}
