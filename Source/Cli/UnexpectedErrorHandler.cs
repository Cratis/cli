// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;
using Cratis.Cli.Commands.Screenplay;

namespace Cratis.Cli;

/// <summary>
/// Handles unexpected command failures while preserving Spectre's parse and cancellation behavior.
/// </summary>
internal static class UnexpectedErrorHandler
{
    internal static int Handle(Exception exception, string[] args, IAnsiConsole console)
    {
        if (exception is OperationCanceledException) return 130;
        if (exception is CommandAppException commandException) return HandleCommandError(commandException, console);

        var settings = SettingsFrom(args);
        var format = settings.ResolveOutputFormat();
        var previousConsole = AnsiConsole.Console;
        try
        {
            AnsiConsole.Console = console;
            WriteUnexpectedError(exception, format);
            if (settings.Debug) console.WriteException(exception);
        }
        finally
        {
            AnsiConsole.Console = previousConsole;
        }
        return ExitCodes.UnexpectedError;
    }

    static void WriteUnexpectedError(Exception exception, string format)
    {
        if (exception is RenderFailedUnexpectedly renderFailure)
        {
            ScreenplayDiagnosticsWriter.Write(format, [new ScreenplayDiagnostic(ScreenplayDiagnosticSeverity.Error, "CLI-RENDER-027", renderFailure.Message, null)]);
            OutputFormatter.WriteError(format, renderFailure.Message, errorCode: ExitCodes.UnexpectedErrorCode);
            return;
        }
        OutputFormatter.WriteError(format, $"{exception.GetType().FullName}: {exception.Message}", errorCode: ExitCodes.UnexpectedErrorCode);
    }

    static int HandleCommandError(CommandAppException exception, IAnsiConsole console)
    {
        if (exception.Pretty is not null)
        {
            console.Write(exception.Pretty);
        }
        else
        {
            console.MarkupLine($"[red]Error:[/] {exception.Message.EscapeMarkup()}");
            if (exception.InnerException is CommandAppException { Pretty: not null } inner) console.Write(inner.Pretty);
        }
        return -1;
    }

    static GlobalSettings SettingsFrom(string[] args)
    {
        var settings = new GlobalSettings();
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--") break;
            var option = args[index].Split('=', 2);
            switch (option[0])
            {
                case "--debug": settings.Debug = true; break;
                case "-q" or "--quiet": settings.Quiet = true; break;
                case "-o" or "--output": settings.Output = option.Length == 2 ? option[1] : args.ElementAtOrDefault(++index) ?? OutputFormats.Auto; break;
            }
        }
        return settings;
    }
}
