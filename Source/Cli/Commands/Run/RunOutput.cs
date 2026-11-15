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
    /// <param name="path">The Screenplay file or folder being run.</param>
    /// <param name="endpoints">The endpoints the session is published on.</param>
    /// <param name="stack">The plan of the run when the model has authentication; its entry point is the address to open.</param>
    public static void WriteHeader(string format, string path, StageEndpoints endpoints, RunStackPlan? stack = null)
    {
        if (IsSilent(format) || IsJson(format))
        {
            return;
        }

        if (string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal))
        {
            Console.WriteLine($"path\t{path}");
            if (stack is not null) Console.WriteLine($"application\t{stack.EntryUrl}");
            Console.WriteLine($"stageApi\t{endpoints.Api}");
            Console.WriteLine($"apiReference\t{endpoints.ApiReference}");
            Console.WriteLine($"workbench\t{endpoints.Workbench}");
            return;
        }

        var muted = OutputFormatter.Muted.ToMarkup();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{muted}]Running the Screenplay from {path.EscapeMarkup()}[/]");
        AnsiConsole.WriteLine();
        if (stack is not null) OutputFormatter.WriteLabel("Application", stack.EntryUrl, LabelWidth);
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
    /// <param name="path">The Screenplay file or folder being run.</param>
    /// <param name="endpoints">The endpoints the session is published on.</param>
    /// <param name="engine">The runtime engine reported by the Stage, if available.</param>
    /// <param name="notes">The non-blocking compiler diagnostics the engine ignored, when reported.</param>
    /// <param name="verbose">Whether to list grouped notes instead of a muted count in table output.</param>
    /// <param name="stack">The plan of the run when the model has authentication, so the services beside the Stage are reported with it.</param>
    /// <param name="container">The name of the container left running when the run was detached, which it is stopped by.</param>
    public static void WriteReady(string format, StageStartup startup, string path, StageEndpoints endpoints, string? engine = null, IReadOnlyList<StageRuntimeNote>? notes = null, bool verbose = false, RunStackPlan? stack = null, string? container = null)
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
                engine,
                notes = notes ?? [],
                path,
                eventModel = startup.EventModel,
                eventStore = startup.EventStore,
                stageApi = endpoints.Api,
                apiReference = endpoints.ApiReference,
                workbench = endpoints.Workbench,
                application = stack?.EntryUrl,
                services = stack?.Services.Select(service => new { name = service.Name, url = service.Url }),
                mailFolder = stack?.MailFolder,
                container
            });
            return;
        }

        if (string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal))
        {
            Console.WriteLine($"eventModel\t{startup.EventModel}");
            Console.WriteLine($"eventStore\t{startup.EventStore}");
            if (engine is not null) Console.WriteLine($"engine\t{engine}");
            foreach (var service in stack?.Services ?? []) Console.WriteLine($"service\t{service.Name}\t{service.Url}");
            if (stack?.MailFolder is { } mail) Console.WriteLine($"mailFolder\t{mail}");
            WriteContainerLine(container);
            Console.WriteLine("status\tready");
            StageDiagnosticOutput.WriteNotes(format, notes, verbose);
            return;
        }

        var muted = OutputFormatter.Muted.ToMarkup();
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Success.ToMarkup()}]✓ Ready[/][{muted}]{RunningModel(startup)}[/]");
        if (engine is not null) OutputFormatter.WriteLabel("Runtime engine", engine, LabelWidth);
        WriteStack(stack);
        StageDiagnosticOutput.WriteNotes(format, notes, verbose);
        AnsiConsole.MarkupLine($"  [{muted}]{StopHint(container)}[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Reports every runtime admission issue and how to address the refused model.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="status">The refused runtime status.</param>
    /// <param name="settings">The original run settings used to build a retry command.</param>
    public static void WriteRefusal(string format, StageRuntimeStatus status, RunSettings? settings = null) =>
        RunRefusalOutput.Write(format, status, settings ?? new RunSettings());

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

    /// <summary>
    /// Lists the keys the environment lacks, without a value. The run does not start.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="environment">The environment that lacks them.</param>
    /// <param name="path">The model that needs them.</param>
    public static void WriteMissingKeys(string format, RunEnvironment environment, string path)
    {
        var keys = string.Join(", ", environment.Missing);
        var message = environment.Exists
            ? $"Environment '{environment.Name}' is missing {environment.Missing.Count} key(s) the model needs: {keys}"
            : $"Environment '{environment.Name}' does not exist, and the model needs {environment.Missing.Count} key(s) from it: {keys}";
        var hint = environment.Exists
            ? $"Set each with 'cratis config set <key> <value> -e {environment.Name}' (add --secret for a secret), or see them all with 'cratis config --missing \"{path}\" -e {environment.Name}'"
            : $"Create it with 'cratis env init {environment.Name}', then set each key with 'cratis config set <key> <value> -e {environment.Name}' (add --secret for a secret)";
        OutputFormatter.WriteError(format, message, hint, ExitCodes.ValidationErrorCode);
    }

    /// <summary>
    /// Reports that a container beside the Stage did not come up, or stopped, along with the last lines it wrote.
    /// </summary>
    /// <param name="format">The output format.</param>
    /// <param name="host">The stack that failed.</param>
    public static void WriteStackFailure(string format, RunStackHost host)
    {
        host.DescribeStopped();
        OutputFormatter.WriteError(format, host.Failure ?? "A container of the application stopped", "Run again with --verbose to see every container's output", ExitCodes.ServerErrorCode);
        if (IsSilent(format) || IsJson(format))
        {
            return;
        }

        var muted = OutputFormatter.Muted.ToMarkup();
        foreach (var line in host.FailureOutput)
        {
            AnsiConsole.MarkupLine($"  [{muted}]{line.EscapeMarkup()}[/]");
        }

        AnsiConsole.WriteLine();
    }

    static void WriteStack(RunStackPlan? stack)
    {
        if (stack is null)
        {
            return;
        }

        foreach (var service in stack.Services)
        {
            OutputFormatter.WriteLabel(service.Name, service.Url, LabelWidth);
        }

        if (stack.MailFolder is { } mail)
        {
            OutputFormatter.WriteLabel("Onboarding emails", mail, LabelWidth);
            AnsiConsole.MarkupLine($"  {new string(' ', LabelWidth)}[{OutputFormatter.Muted.ToMarkup()}]Written as .eml files, never sent — the environment names no mail server[/]");
        }
    }

    static string RunningModel(StageStartup startup) =>
        startup is { EventModel.Length: > 0, EventStore.Length: > 0 }
            ? $" — event model '{startup.EventModel.EscapeMarkup()}' as event store '{startup.EventStore.EscapeMarkup()}'"
            : string.Empty;

    static void WriteContainerLine(string? container)
    {
        if (container is not null) Console.WriteLine($"container\t{container}");
    }

    static string StopHint(string? container) =>
        container is null ? "Press Ctrl+C to stop" : $"Left running — stop it with: cratis stop {container.EscapeMarkup()}";

    static bool IsSilent(string format) =>
        string.Equals(format, OutputFormats.Quiet, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.JsonQuiet, StringComparison.Ordinal);

    static bool IsJson(string format) =>
        string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal);
}
