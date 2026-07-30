// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Renders what a Stage session reports — where it can be reached, when it is ready, and what went wrong.
/// </summary>
public static class RunOutput
{
    /// <summary>
    /// The number of captured output lines to show when the container fails before becoming ready.
    /// </summary>
    public const int FailureOutputLines = 20;

    const int LabelWidth = 20;

    /// <summary>
    /// Writes the endpoints the session will be reachable on, before it starts.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="path">The folder of Screenplay files being run.</param>
    /// <param name="endpoints">The endpoints the session is published on.</param>
    public static void WriteHeader(string format, string path, StageEndpoints endpoints)
    {
        if (IsSilent(format) || IsJson(format))
        {
            return;
        }

        if (string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal))
        {
            Console.WriteLine($"path\t{path}");
            Console.WriteLine($"stageApi\t{endpoints.Api}");
            Console.WriteLine($"apiReference\t{endpoints.ApiReference}");
            Console.WriteLine($"workbench\t{endpoints.Workbench}");
            return;
        }

        var muted = OutputFormatter.Muted.ToMarkup();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{muted}]Running the Screenplay files in {path.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
        OutputFormatter.WriteLabel("Stage API", endpoints.Api, LabelWidth);
        OutputFormatter.WriteLabel("API reference", endpoints.ApiReference, LabelWidth);
        OutputFormatter.WriteLabel("Chronicle Workbench", endpoints.Workbench, LabelWidth);
        AnsiConsole.MarkupLine($"  {new string(' ', LabelWidth)}[{muted}]HTTPS only — sign in with {StageEndpoints.WorkbenchUser} / {StageEndpoints.WorkbenchPassword.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Builds the progress text shown while the container starts.
    /// </summary>
    /// <param name="startup">The startup being tracked.</param>
    /// <param name="elapsed">The time the container has been starting.</param>
    /// <returns>The progress text.</returns>
    public static string StatusText(StageStartup startup, TimeSpan elapsed) =>
        $"{startup.Status.EscapeMarkup()}... [{OutputFormatter.Muted.ToMarkup()}]{elapsed.TotalSeconds:F0}s[/]";

    /// <summary>
    /// Writes that the session is ready to accept requests.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="startup">The startup the container reported.</param>
    /// <param name="path">The folder of Screenplay files being run.</param>
    /// <param name="endpoints">The endpoints the session is published on.</param>
    public static void WriteReady(string format, StageStartup startup, string path, StageEndpoints endpoints)
    {
        if (IsSilent(format))
        {
            return;
        }

        if (IsJson(format))
        {
            OutputFormatter.WriteObject(format, new
            {
                status = "ready",
                path,
                eventModel = startup.EventModel,
                eventStore = startup.EventStore,
                stageApi = endpoints.Api,
                apiReference = endpoints.ApiReference,
                workbench = endpoints.Workbench
            });
            return;
        }

        if (string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal))
        {
            Console.WriteLine($"eventModel\t{startup.EventModel}");
            Console.WriteLine($"eventStore\t{startup.EventStore}");
            Console.WriteLine("status\tready");
            return;
        }

        var muted = OutputFormatter.Muted.ToMarkup();
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Success.ToMarkup()}]✓ Ready[/][{muted}]{RunningModel(startup)}[/]");
        AnsiConsole.MarkupLine($"  [{muted}]Press Ctrl+C to stop[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Writes that the session is being stopped.
    /// </summary>
    /// <param name="format">The output format.</param>
    public static void WriteStopping(string format)
    {
        if (IsSilent(format) || IsJson(format) || string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal))
        {
            return;
        }

        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]Stopping the Stage...[/]");
    }

    /// <summary>
    /// Writes that the container is still stopping after being given time to shut down.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="timeout">The time the container was given to stop.</param>
    public static void WriteStopTimedOut(string format, TimeSpan timeout)
    {
        if (IsSilent(format) || IsJson(format) || string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal))
        {
            return;
        }

        AnsiConsole.MarkupLine($"  [{OutputFormatter.Warning.ToMarkup()}]The container did not stop within {timeout.TotalSeconds:F0} seconds — check 'docker ps'[/]");
    }

    /// <summary>
    /// Writes that the container stopped before it became ready, along with the output it produced.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="session">The session that failed.</param>
    public static void WriteFailure(string format, StageSession session)
    {
        var error = session.Startup.Error ?? "The Stage container stopped before it was ready";
        OutputFormatter.WriteError(format, error, "Run again with --verbose to see the container's full output", ExitCodes.ServerErrorCode);

        if (IsSilent(format) || IsJson(format))
        {
            return;
        }

        var lines = session.Output.Tail(FailureOutputLines);
        if (lines.Count == 0)
        {
            return;
        }

        var muted = OutputFormatter.Muted.ToMarkup();
        foreach (var line in lines)
        {
            AnsiConsole.MarkupLine($"  [{muted}]{line.EscapeMarkup()}[/]");
        }

        AnsiConsole.WriteLine();
    }

    static string RunningModel(StageStartup startup) =>
        startup is { EventModel.Length: > 0, EventStore.Length: > 0 }
            ? $" — event model '{startup.EventModel.EscapeMarkup()}' as event store '{startup.EventStore.EscapeMarkup()}'"
            : string.Empty;

    static bool IsSilent(string format) =>
        string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.JsonQuiet, StringComparison.Ordinal);

    static bool IsJson(string format) =>
        string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal);
}
