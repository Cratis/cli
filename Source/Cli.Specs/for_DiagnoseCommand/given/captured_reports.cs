// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Spectre.Console;

namespace Cratis.Cli.for_DiagnoseCommand.given;

public class captured_reports : Specification
{
    protected DiagnoseData _data;
    protected readonly Dictionary<string, string> _outputs = new(StringComparer.Ordinal);

    void Establish() => _data = new DiagnoseData("chronicle://localhost:35000", "store", "tenant-one", true, null, null, ["store"], 0, 0, 0, 0, 0, 0, 10, DateTimeOffset.UtcNow);

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
                    Ansi = AnsiSupport.No
                });
                AnsiConsole.Console.Profile.Width = 240;
                if (format == "watch")
                {
                    AnsiConsole.Write(DiagnoseCommand.BuildWatchReport(_data));
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
