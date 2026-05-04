// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle;

namespace Cratis.Cli.Commands.GetStarted;

/// <summary>
/// Context-aware onboarding and diagnostic guide.
/// Shows active context status, tests the server connection, and displays useful commands and debugging workflows.
/// </summary>
[LlmDescription("Shows setup status, server connection health, and useful starter commands. Use to verify the CLI is configured correctly or when exploring Chronicle for the first time.")]
[CliCommand("get-started", "Show setup status, connection health, and useful commands to explore Chronicle")]
[CliExample("get-started")]
[CliExample("get-started", "-o", "json")]
[LlmOutputAdvice("json", "JSON contains activeContext, server, eventStore, namespace, auth, canConnect, and otherContexts — useful for programmatic diagnostics.")]
public class GetStartedCommand : AsyncCommand<ChronicleSettings>
{
    /// <inheritdoc/>
    protected override async Task<int> ExecuteAsync(CommandContext context, ChronicleSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();
        var accent = OutputFormatter.Accent.ToMarkup();
        var muted = OutputFormatter.Muted.ToMarkup();
        var success = OutputFormatter.Success.ToMarkup();
        var warning = OutputFormatter.Warning.ToMarkup();

        var configPath = CliConfiguration.GetConfigPath();
        var hasConfig = File.Exists(configPath);

        if (!hasConfig)
        {
            if (IsJsonFormat(format))
            {
                OutputFormatter.WriteObject(format, new { configured = false });
                return ExitCodes.Success;
            }

            RenderNoConfig(accent, muted);
            return ExitCodes.Success;
        }

        var config = CliConfiguration.Load();
        var ctx = config.GetCurrentContext();
        var contextName = config.ActiveContextName;

        var resolvedServer = settings.ResolveConnectionString();
        var managementPort = settings.ResolveManagementPort();

        // The display server shown to the user should be the raw context value (without injected credentials).
        var envServer = Environment.GetEnvironmentVariable(CliDefaults.ConnectionStringEnvVar);
        string displayServer;
        if (!string.IsNullOrWhiteSpace(envServer))
        {
            displayServer = envServer;
        }
        else if (!string.IsNullOrWhiteSpace(ctx.Server))
        {
            displayServer = ctx.Server;
        }
        else
        {
            displayServer = "chronicle://localhost:35000/?disableTls=true";
        }

        var authState = ResolveAuthState(ctx);

        // Attempt connection — use the same 5s timeout as other CLI commands.
        var canConnect = false;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            var connectionString = new ChronicleConnectionString(resolvedServer);
            using var connection = await CliChronicleConnection.Connect(connectionString, managementPort, cts.Token);
            canConnect = true;
        }
        catch
        {
            canConnect = false;
        }

        if (IsJsonFormat(format))
        {
            var otherContexts = config.Contexts
                .Where(kvp => kvp.Key != contextName)
                .Select(kvp => new { name = kvp.Key, server = kvp.Value.Server ?? "(not set)" })
                .ToArray();

            OutputFormatter.WriteObject(format, new
            {
                activeContext = contextName,
                server = displayServer,
                eventStore = ctx.EventStore ?? CliDefaults.DefaultEventStoreName,
                @namespace = ctx.Namespace ?? CliDefaults.DefaultNamespaceName,
                auth = authState,
                loggedInUser = ctx.LoggedInUser,
                canConnect,
                otherContexts
            });
            return ExitCodes.Success;
        }

        RenderContextPanel(contextName, displayServer, ctx, authState, canConnect, accent, muted, success, warning);

        if (!canConnect && config.Contexts.Count > 1)
        {
            RenderOtherContexts(config, contextName, accent, muted);
        }

        RenderExplorePanel(accent);
        RenderDebugPanel(accent, muted);
        RenderTipsPanel(accent);

        return ExitCodes.Success;
    }

    static bool IsJsonFormat(string format) =>
        string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) ||
        string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal);

    static string ResolveAuthState(CliContext ctx)
    {
        if (!string.IsNullOrWhiteSpace(ctx.LoggedInUser))
        {
            return $"Logged in as {ctx.LoggedInUser}";
        }

        if (!string.IsNullOrWhiteSpace(ctx.ClientId))
        {
            return "Client credentials configured";
        }

        return "No auth configured";
    }

    static void RenderNoConfig(string accent, string muted)
    {
        var content = new Markup(
            "  No configuration found. Set up a context to get started:\n\n" +
            $"  [{accent}]1.[/] Create a context pointing at your server:\n" +
            "     [bold]cratis context create dev \\\n" +
            "       --server chronicle://localhost:35000/?disableTls=true[/]\n\n" +
            $"  [{accent}]2.[/] Verify the connection:\n" +
            "     [bold]cratis get-started[/]\n\n" +
            $"  [{accent}]3.[/] Start exploring:\n" +
            "     [bold]cratis chronicle event-stores list[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(content)
            .Header($"[{accent}] Getting Started [/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(OutputFormatter.Accent))
            .Padding(1, 1));
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"  [{muted}]Run [bold]cratis --help[/] to see all available commands.[/]");
        AnsiConsole.WriteLine();
    }

    static void RenderContextPanel(string contextName, string resolvedServer, CliContext ctx, string authState, bool canConnect, string accent, string muted, string success, string warning)
    {
        var connectionStatus = canConnect
            ? $"[{success}]✓ Reachable[/]"
            : $"[{warning}]✗ Could not connect[/]";

        var content = new Markup(
            $"  [bold]Context[/]      {contextName.EscapeMarkup()}\n" +
            $"  [bold]Server[/]       {resolvedServer.EscapeMarkup()}\n" +
            $"  [bold]Event Store[/]  {(string.IsNullOrWhiteSpace(ctx.EventStore) ? $"[{muted}]{CliDefaults.DefaultEventStoreName}[/]" : ctx.EventStore.EscapeMarkup())}\n" +
            $"  [bold]Namespace[/]    {(string.IsNullOrWhiteSpace(ctx.Namespace) ? $"[{muted}]{CliDefaults.DefaultNamespaceName}[/]" : ctx.Namespace.EscapeMarkup())}\n" +
            $"  [bold]Auth[/]         {authState.EscapeMarkup()}\n" +
            $"  [bold]Connection[/]   {connectionStatus}");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(content)
            .Header($"[{accent}] Active Context [/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(OutputFormatter.Accent))
            .Padding(1, 1));
    }

    static void RenderOtherContexts(CliConfiguration config, string activeContextName, string accent, string muted)
    {
        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderStyle(new Style(OutputFormatter.Muted))
            .AddColumn(new TableColumn($"[{accent}]Context[/]"))
            .AddColumn(new TableColumn($"[{accent}]Server[/]"));

        foreach (var (name, ctx) in config.Contexts.Where(kvp => kvp.Key != activeContextName))
        {
            table.AddRow(
                name.EscapeMarkup(),
                (ctx.Server ?? $"[{muted}]—[/]").EscapeMarkup());
        }

        var hint = new Markup(
            $"  [{muted}]Switch context with:[/] [bold]cratis context set <name>[/]");

        var content = new Rows(
            new Markup($"  [{muted}]Could not reach the server. Other configured contexts:[/]\n"),
            table,
            new Text(string.Empty),
            hint);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(content)
            .Header($"[{OutputFormatter.Warning.ToMarkup()}] Connection Failed [/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(OutputFormatter.Warning))
            .Padding(1, 1));
    }

    static void RenderExplorePanel(string accent)
    {
        var table = new Table()
            .Border(TableBorder.Simple)
            .BorderStyle(new Style(OutputFormatter.Muted))
            .AddColumn(new TableColumn($"[{accent}]Command[/]"))
            .AddColumn(new TableColumn($"[{accent}]Description[/]"));

        var commands = new (string Command, string Description)[]
        {
            ("cratis chronicle event-stores list",       "List all event stores on the server"),
            ("cratis chronicle namespaces list",         "List namespaces in the active event store"),
            ("cratis chronicle observers list",          "List all observers (reactors, reducers, projections)"),
            ("cratis chronicle events get",              "Fetch events from the event log"),
            ("cratis chronicle projections list",        "List all projection definitions"),
            ("cratis chronicle read-models instances",   "Browse read model instances"),
            ("cratis chronicle jobs list",               "List running and completed jobs"),
            ("cratis chronicle failed-partitions list",  "List failed observer partitions"),
            ("cratis chronicle recommendations list",    "List server-generated recommendations"),
        };

        foreach (var (cmd, desc) in commands)
        {
            table.AddRow($"[bold]{cmd.EscapeMarkup()}[/]", desc.EscapeMarkup());
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(table)
            .Header($"[{accent}] Explore [/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(OutputFormatter.Accent))
            .Padding(1, 1));
    }

    static void RenderDebugPanel(string accent, string muted)
    {
        var content = new Markup(
            $"  [{accent}]Check system health[/]\n" +
            $"    [bold]cratis chronicle diagnose[/]                                      [{muted}]Full health report[/]\n" +
            $"    [bold]cratis chronicle diagnose --watch[/]                              [{muted}]Live-refresh loop[/]\n\n" +

            $"  [{accent}]Observers not processing / stuck[/]\n" +
            $"    [bold]cratis chronicle observers list[/]                                [{muted}]Check running state and last-handled sequence[/]\n" +
            $"    [bold]cratis chronicle failed-partitions list[/]                        [{muted}]Any partition errors?[/]\n" +
            $"    [bold]cratis chronicle failed-partitions show <observer-id> <partition>[/]  [{muted}]Full error + stack trace[/]\n" +
            $"    [bold]cratis chronicle observers retry-partition <id> <partition>[/]    [{muted}]Retry without full replay[/]\n" +
            $"    [bold]cratis chronicle observers replay <id>[/]                         [{muted}]Last resort: replay from sequence 0[/]\n\n" +

            $"  [{accent}]Read model data stale or wrong[/]\n" +
            $"    [bold]cratis chronicle read-models instances <name>[/]                  [{muted}]Browse current data[/]\n" +
            $"    [bold]cratis chronicle read-models get <name> <key>[/]                  [{muted}]Inspect one instance (event count, last seq)[/]\n" +
            $"    [bold]cratis chronicle projections show <id>[/]                         [{muted}]Inspect the projection declaration[/]\n" +
            $"    [bold]cratis chronicle observers replay <id>[/]                         [{muted}]Rebuild the read model from scratch[/]\n\n" +

            $"  [{accent}]Stuck jobs or server recommendations[/]\n" +
            $"    [bold]cratis chronicle jobs list[/]                                     [{muted}]See running / failed jobs[/]\n" +
            $"    [bold]cratis chronicle jobs resume <id>[/]                              [{muted}]Unblock a stopped job[/]\n" +
            $"    [bold]cratis chronicle recommendations list[/]                          [{muted}]Server-generated suggestions[/]\n" +
            $"    [bold]cratis chronicle recommendations perform <id>[/]                  [{muted}]Act on a recommendation[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(content)
            .Header($"[{accent}] Debugging Scenarios [/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(OutputFormatter.Accent))
            .Padding(1, 1));
    }

    static void RenderTipsPanel(string accent)
    {
        var content = new Markup(
            $"  [{accent}]Output formats[/]  add [bold]-o json[/], [bold]-o plain[/], or [bold]-o table[/] to any command\n\n" +
            $"  [{accent}]Shell completions[/]  [bold]cratis completions install bash[/]  (or [bold]zsh[/] / [bold]fish[/])\n\n" +
            $"  [{accent}]AI tools[/]  run [bold]cratis init[/] to generate [bold]CHRONICLE.md[/] and configure Claude / Copilot / Cursor\n\n" +
            $"  [{accent}]All commands[/]  [bold]cratis --help[/]");

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(content)
            .Header($"[{accent}] Tips [/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(new Style(OutputFormatter.Accent))
            .Padding(1, 1));
        AnsiConsole.WriteLine();
    }
}
