// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Chronicle.Diagnose;

/// <summary>
/// Runs a battery of health checks against the Chronicle server and renders a diagnostic report.
/// Supports a live --watch mode that refreshes the report on a configurable interval.
/// </summary>
[LlmDescription("Runs a health check against the Chronicle server and returns a diagnostic report covering connectivity, event store health, and configuration. Use to debug connection or server issues.")]
[CommandEffect(CommandEffect.ReadOnly)]
[CliCommand("diagnose", "Run a health check against the Chronicle server and show a diagnostic report", Branch = typeof(ChronicleBranch))]
[CliExample("chronicle", "diagnose")]
[CliExample("chronicle", "diagnose", "-o", "json")]
[CliExample("chronicle", "diagnose", "--all-namespaces", "-o", "json")]
[CliExample("chronicle", "diagnose", "--all-event-stores", "-o", "json")]
[CliExample("chronicle", "diagnose", "--watch")]
[CliExample("chronicle", "diagnose", "--watch", "--interval", "2")]
public partial class DiagnoseCommand : ChronicleCommand<DiagnoseSettings>
{
    /// <summary>
    /// Checks for a newer server version, leaving the refresh the check may start with the caller that waits for it.
    /// </summary>
    /// <param name="serverVersion">The version of the server.</param>
    /// <param name="check">Checks a package for a newer version, given the package, the current version, where to register the refresh and a token.</param>
    /// <param name="cancellationToken">A cancellation token for timeout control.</param>
    /// <returns>The latest server version if newer, otherwise null.</returns>
    internal static Task<string?> CheckLatestServerVersion(
        string serverVersion,
        Func<string, string, VersionRefreshes?, CancellationToken, Task<string?>> check,
        CancellationToken cancellationToken) =>
        check(UpdateChecker.ServerPackageId, serverVersion, VersionRefreshes.Current, cancellationToken);

    internal static Table BuildWatchReport(DiagnoseData data, int intervalSeconds = 5, bool pending = false) => BuildLiveTable(data, intervalSeconds, pending);

    internal static void Render(string format, DiagnoseData data)
    {
        if (string.Equals(format, OutputFormats.Json, StringComparison.Ordinal) ||
            string.Equals(format, OutputFormats.JsonCompact, StringComparison.Ordinal) ||
            string.Equals(format, OutputFormats.JsonQuiet, StringComparison.Ordinal))
        {
            OutputFormatter.WriteObject(format, new
            {
                capturedAt = data.CapturedAt,
                healthy = data.IsHealthy,
                checksComplete = data.ChecksComplete,
                checksCouldNotRun = data.ChecksCouldNotRun,
                findings = data.Findings,
                scopes = data.Scopes.Select(scope => new
                {
                    eventStore = scope.EventStore,
                    @namespace = scope.Namespace,
                    healthy = scope.IsHealthy,
                    checksComplete = scope.ChecksComplete,
                    observers = new
                    {
                        total = scope.TotalObservers,
                        active = scope.ActiveObservers,
                        replaying = scope.ReplayingObservers,
                        suspended = scope.SuspendedObservers,
                        disconnected = scope.DisconnectedObservers,
                        quarantined = scope.QuarantinedObservers
                    },
                    failedPartitions = scope.FailedPartitions,
                    pendingRecommendations = scope.PendingRecommendations,
                    eventSequenceTail = scope.EventSequenceTail
                }),
                connection = new
                {
                    server = ConnectionStringRedaction.Redact(data.ConnectionString),
                    reachable = data.ServerReachable
                },
                version = new
                {
                    server = data.ServerVersion,
                    latestServer = data.LatestServerVersion
                },
                eventStores = data.EventStores,
                observers = new
                {
                    total = data.TotalObservers,
                    active = data.ActiveObservers,
                    replaying = data.ReplayingObservers,
                    suspended = data.SuspendedObservers,
                    disconnected = data.DisconnectedObservers,
                    quarantined = data.QuarantinedObservers
                },
                failedPartitions = data.FailedPartitions,
                pendingRecommendations = data.PendingRecommendations,
                eventSequenceTail = data.EventSequenceTail
            });
            return;
        }

        if (string.Equals(format, OutputFormats.Plain, StringComparison.Ordinal))
        {
            RenderPlain(data);
            return;
        }

        RenderText(data);
        if (!data.IsHealthy &&
            (string.Equals(format, OutputFormats.Table, StringComparison.Ordinal) || string.Equals(format, OutputFormats.Auto, StringComparison.Ordinal)))
        {
            WriteCommunityHint();
        }
    }

    internal static async Task<int> RunWatch(IServices services, DiagnoseSettings settings, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        void CancelHandler(object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            cts.Cancel();
        }

        var interval = settings.Interval;
        DiagnoseData? lastData = null;
        var initialData = new DiagnoseData(
            ConnectionString: settings.ResolveConnectionString(),
            EventStore: settings.AllEventStores ? "all event stores" : settings.ResolveEventStore(),
            Namespace: settings.AllNamespaces || settings.AllEventStores ? "all namespaces" : settings.ResolveNamespace(),
            ServerReachable: false,
            ServerVersion: null,
            LatestServerVersion: null,
            EventStores: [],
            ActiveObservers: 0,
            ReplayingObservers: 0,
            SuspendedObservers: 0,
            DisconnectedObservers: 0,
            FailedPartitions: 0,
            PendingRecommendations: 0,
            EventSequenceTail: null,
            CapturedAt: DateTimeOffset.Now)
        {
            IsAggregate = settings.AllNamespaces || settings.AllEventStores
        };

        Console.CancelKeyPress += CancelHandler;
        try
        {
            await AnsiConsole.Live(BuildWatchReport(initialData, interval, pending: true))
                .StartAsync(async ctx =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var data = await Gather(services, settings);
                        lastData = data;
                        ctx.UpdateTarget(BuildWatchReport(data, interval));
                        ctx.Refresh();

                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(settings.Interval), cts.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                });
        }
        finally
        {
            Console.CancelKeyPress -= CancelHandler;
        }

        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]Watch stopped.[/]");
        if (lastData is { IsHealthy: false })
        {
            WriteCommunityHint();
        }

        return lastData?.ExitCode ?? ExitCodes.ServerError;
    }

    static void WriteCommunityHint() =>
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]Questions? Ask on Discord: https://discord.gg/kt4AMpV8WV[/]");

    static void RenderText(DiagnoseData data)
    {
        var redactedConnectionString = ConnectionStringRedaction.Redact(data.ConnectionString);
        var serverText = data.ServerReachable
            ? $"[bold]{redactedConnectionString.EscapeMarkup()}[/]"
            : $"[{OutputFormatter.Danger.ToMarkup()}]{redactedConnectionString.EscapeMarkup()} (unreachable)[/]";

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[bold]Chronicle Diagnostics[/]  [{OutputFormatter.Muted.ToMarkup()}]{data.CapturedAt:HH:mm:ss}[/]")
            .RuleStyle(new Style(OutputFormatter.Muted))
            .LeftJustified());

        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]server:[/]      {serverText}");
        AnsiConsole.MarkupLine($"  [{OutputFormatter.Muted.ToMarkup()}]event store:[/] {data.EventStore.EscapeMarkup()}  [{OutputFormatter.Muted.ToMarkup()}]/[/]  {data.Namespace.EscapeMarkup()}");
        AnsiConsole.WriteLine();

        WriteCheck(data.ServerReachable, "Connection", data.ServerReachable ? "connected" : "unreachable");

        if (data.ServerReachable)
        {
            if (data.LatestServerVersion is not null)
            {
                var serverVersionText = (data.ServerVersion ?? "unknown").EscapeMarkup();
                var latestVersionText = data.LatestServerVersion.EscapeMarkup();
                WriteCheck(false, "Server version", $"{serverVersionText}  [{OutputFormatter.Warning.ToMarkup()}]update available: {latestVersionText}[/]  [{OutputFormatter.Muted.ToMarkup()}]→ upgrade the Chronicle server[/]", isWarning: true);
            }
            else
            {
                WriteCheck(true, "Server version", data.ServerVersion ?? "unknown");
            }
        }

        var eventStoreStatus = data.EventStores.Count switch
        {
            0 => $"[{OutputFormatter.Muted.ToMarkup()}]none found[/]",
            1 => $"{data.EventStores[0].EscapeMarkup()}",
            _ => $"{data.EventStores.Count} stores: {string.Join(", ", data.EventStores.Select(e => e.EscapeMarkup()))}"
        };
        WriteCheck(data.EventStores.Count > 0 && CheckCompleted(data, "Event stores"), "Event stores", CheckDetail(data, "Event stores", eventStoreStatus));

        var observerStatus = data.TotalObservers == 0
            ? $"[{OutputFormatter.Muted.ToMarkup()}]none; 0 quarantined[/]"
            : BuildObserverStatus(data);
        WriteCheck(data.QuarantinedObservers == 0 && CheckCompleted(data, "Observers"), "Observers", CheckDetail(data, "Observers", observerStatus));
        WriteCheck(data.QuarantinedObservers == 0 && CheckCompleted(data, "Observers"), "Quarantined observers", CheckDetail(data, "Observers", $"{data.QuarantinedObservers} quarantined (known count)"));

        var failedPartitionStatus = data.FailedPartitions == 0
            ? $"[{OutputFormatter.Success.ToMarkup()}]none[/]"
            : $"[{OutputFormatter.Danger.ToMarkup()}]{data.FailedPartitions} need attention[/]  [{OutputFormatter.Muted.ToMarkup()}]→ cratis chronicle failed-partitions list[/]";
        WriteCheck(data.FailedPartitions == 0 && CheckCompleted(data, "Failed partitions"), "Failed partitions", CheckDetail(data, "Failed partitions", failedPartitionStatus));

        var recsStatus = data.PendingRecommendations == 0
            ? $"[{OutputFormatter.Success.ToMarkup()}]none[/]"
            : $"[{OutputFormatter.Warning.ToMarkup()}]{data.PendingRecommendations} pending[/]  [{OutputFormatter.Muted.ToMarkup()}]→ cratis chronicle recommendations list[/]";
        WriteCheck(data.PendingRecommendations == 0 && CheckCompleted(data, "Recommendations"), "Recommendations", CheckDetail(data, "Recommendations", recsStatus));

        var tailStatus = data.EventSequenceTail.HasValue
            ? $"tail: {data.EventSequenceTail.Value:N0}"
            : $"[{OutputFormatter.Muted.ToMarkup()}]empty[/]";
        if (data.IsAggregate)
        {
            tailStatus = "per scope (see below)";
        }

        var tailChecked = CheckCompleted(data, "Event sequence");
        WriteCheck(tailChecked && (data.EventSequenceTail.HasValue || data.IsAggregate), "Event sequence", CheckDetail(data, "Event sequence", tailStatus), isInfo: tailChecked);
        RenderFindings(data);
        RenderIncompleteChecks(data);

        AnsiConsole.WriteLine();

        if (data.IsHealthy)
        {
            AnsiConsole.MarkupLine($"  [{OutputFormatter.Success.ToMarkup()}]✓ System is healthy[/]");
        }
        else
        {
            var status = data.ChecksComplete ? "Issues detected" : "Health check incomplete";
            AnsiConsole.MarkupLine($"  [{OutputFormatter.Danger.ToMarkup()}]✗ {status} — review items above[/]");
        }

        AnsiConsole.WriteLine();
    }

    static string BuildObserverStatus(DiagnoseData data)
    {
        var parts = new List<string>();

        if (data.ActiveObservers > 0)
        {
            parts.Add($"[{OutputFormatter.Success.ToMarkup()}]{data.ActiveObservers} active[/]");
        }

        if (data.ReplayingObservers > 0)
        {
            parts.Add($"[{OutputFormatter.Success.ToMarkup()}]{data.ReplayingObservers} replaying[/]");
        }

        if (data.SuspendedObservers > 0)
        {
            parts.Add($"[{OutputFormatter.Muted.ToMarkup()}]{data.SuspendedObservers} suspended[/]");
        }

        if (data.DisconnectedObservers > 0)
        {
            parts.Add($"[{OutputFormatter.Warning.ToMarkup()}]{data.DisconnectedObservers} disconnected[/]");
        }

        parts.Add($"[{(data.QuarantinedObservers > 0 ? OutputFormatter.Danger : OutputFormatter.Muted).ToMarkup()}]{data.QuarantinedObservers} quarantined[/]");

        return string.Join("  ", parts);
    }

    static void WriteCheck(bool ok, string label, string detail, bool isInfo = false, bool isWarning = false)
    {
        string icon;
        if (isWarning)
        {
            icon = $"[{OutputFormatter.Warning.ToMarkup()}]▲[/]";
        }
        else if (ok)
        {
            icon = $"[{OutputFormatter.Success.ToMarkup()}]✓[/]";
        }
        else if (isInfo)
        {
            icon = $"[{OutputFormatter.Muted.ToMarkup()}]·[/]";
        }
        else
        {
            icon = $"[{OutputFormatter.Danger.ToMarkup()}]✗[/]";
        }

        AnsiConsole.MarkupLine($"  {icon}  [{OutputFormatter.Accent.ToMarkup()}]{label.PadRight(21).EscapeMarkup()}[/]  {detail}");
    }

    static void RenderPlain(DiagnoseData data)
    {
        Console.WriteLine($"healthy={data.IsHealthy}");
        Console.WriteLine($"checks_complete={data.ChecksComplete}");
        Console.WriteLine($"checks_could_not_run={data.ChecksCouldNotRun.Count}");
        Console.WriteLine($"server={PlainValue(ConnectionStringRedaction.Redact(data.ConnectionString))}");
        Console.WriteLine($"reachable={data.ServerReachable}");
        Console.WriteLine($"server_version={PlainValue(data.ServerVersion)}");
        Console.WriteLine($"server_version_latest={PlainValue(data.LatestServerVersion)}");
        Console.WriteLine($"event_stores={data.EventStores.Count}");
        Console.WriteLine($"observers_active={data.ActiveObservers}");
        Console.WriteLine($"observers_replaying={data.ReplayingObservers}");
        Console.WriteLine($"observers_suspended={data.SuspendedObservers}");
        Console.WriteLine($"observers_disconnected={data.DisconnectedObservers}");
        Console.WriteLine($"observers_quarantined={data.QuarantinedObservers}");
        Console.WriteLine($"failed_partitions={data.FailedPartitions}");
        Console.WriteLine($"pending_recommendations={data.PendingRecommendations}");
        Console.WriteLine($"event_sequence_tail={data.EventSequenceTail?.ToString() ?? string.Empty}");
        foreach (var check in data.ChecksCouldNotRun)
        {
            Console.WriteLine($"could_not_check={PlainValue(check.Check)} event_store={PlainValue(check.EventStore)} namespace={PlainValue(check.Namespace)} reason={PlainValue(check.Reason)}");
        }

        foreach (var finding in data.Findings)
        {
            Console.WriteLine($"finding={PlainValue(finding.Check)} event_store={PlainValue(finding.EventStore)} namespace={PlainValue(finding.Namespace)} detail={PlainValue(finding.Detail)}");
        }

        foreach (var scope in data.Scopes.Where(_ => ShowScopeTails(data)))
        {
            Console.WriteLine($"scope_event_sequence_tail={scope.EventSequenceTail?.ToString() ?? string.Empty} event_store={PlainValue(scope.EventStore)} namespace={PlainValue(scope.Namespace)}");
        }
    }

    static string PlainValue(string? value)
    {
        var text = value?.Replace('\r', ' ').Replace('\n', ' ') ?? string.Empty;
        return text.Any(c => char.IsWhiteSpace(c) || c is '=' or '"' or '\\')
            ? $"\"{text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : text;
    }

    static string WatchOverflowDetail(int count, string items) => $"+{count} more → rerun without --watch for all {items}";

    static Table BuildLiveTable(DiagnoseData data, int intervalSeconds = 5, bool pending = false)
    {
        const int maximumRows = 3;
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderColor(OutputFormatter.Muted)
            .AddColumn(new TableColumn(string.Empty).Width(3).NoWrap())
            .AddColumn(new TableColumn($"[bold]{data.EventStore.EscapeMarkup()}[/]  [{OutputFormatter.Muted.ToMarkup()}]/{data.Namespace.EscapeMarkup()}[/]").NoWrap())
            .AddColumn(new TableColumn($"[{OutputFormatter.Muted.ToMarkup()}]{data.CapturedAt:HH:mm:ss}  (every {intervalSeconds}s)[/]").RightAligned());

        var pendingIcon = $"[{OutputFormatter.Muted.ToMarkup()}]·[/]";
        if (pending)
        {
            foreach (var label in new[] { "Health", "Connection", "Observers", "Quarantined observers", "Failed partitions", "Recommendations", "Event sequence tail" })
            {
                table.AddRow(pendingIcon, $"[{OutputFormatter.Accent.ToMarkup()}]{label}[/]", "checking…");
            }

            return table;
        }

        var health = data.IsHealthy ? "healthy" : "issues detected";
        if (!data.ChecksComplete)
        {
            health = "check incomplete";
        }

        var healthIcon = data.IsHealthy ? $"[{OutputFormatter.Success.ToMarkup()}]✓[/]" : $"[{OutputFormatter.Danger.ToMarkup()}]✗[/]";
        var unavailableIcon = $"[{OutputFormatter.Danger.ToMarkup()}]✗[/]";
        table.AddRow(healthIcon, "Health", health);
        foreach (var check in data.ChecksCouldNotRun.Take(maximumRows))
        {
            table.AddRow(unavailableIcon, "Could not check", DescribeCheckFailure(check).EscapeMarkup());
        }

        if (data.ChecksCouldNotRun.Count > maximumRows)
        {
            table.AddRow(unavailableIcon, "Could not check", WatchOverflowDetail(data.ChecksCouldNotRun.Count - maximumRows, "checks"));
        }

        var connectionIcon = data.ServerReachable ? $"[{OutputFormatter.Success.ToMarkup()}]✓[/]" : unavailableIcon;
        var connectionDetail = data.ServerReachable ? "connected" : $"[{OutputFormatter.Danger.ToMarkup()}]unreachable[/]";
        table.AddRow(
            connectionIcon,
            $"[{OutputFormatter.Accent.ToMarkup()}]Connection[/]",
            connectionDetail);

        if (data.ServerReachable)
        {
            var serverVersionCell = data.LatestServerVersion is not null
                ? $"{(data.ServerVersion ?? "unknown").EscapeMarkup()}  [{OutputFormatter.Warning.ToMarkup()}]↑ {data.LatestServerVersion.EscapeMarkup()}[/]"
                : (data.ServerVersion ?? "unknown").EscapeMarkup();
            var serverVersionIcon = data.LatestServerVersion is not null
                ? $"[{OutputFormatter.Warning.ToMarkup()}]▲[/]"
                : $"[{OutputFormatter.Muted.ToMarkup()}]·[/]";
            table.AddRow(serverVersionIcon, $"[{OutputFormatter.Accent.ToMarkup()}]Server version[/]", serverVersionCell);
        }

        var observersIcon = data.QuarantinedObservers == 0 && CheckCompleted(data, "Observers")
            ? $"[{OutputFormatter.Success.ToMarkup()}]✓[/]"
            : $"[{OutputFormatter.Danger.ToMarkup()}]✗[/]";
        var observersDetail = data.TotalObservers == 0 ? $"[{OutputFormatter.Muted.ToMarkup()}]none; 0 quarantined[/]" : BuildObserverStatus(data);
        table.AddRow(observersIcon, $"[{OutputFormatter.Accent.ToMarkup()}]Observers[/]", CheckDetail(data, "Observers", observersDetail, reasonsAbove: true));
        table.AddRow(observersIcon, "Quarantined observers", CheckDetail(data, "Observers", $"{data.QuarantinedObservers} quarantined (known count)", reasonsAbove: true));

        var failedIcon = data.FailedPartitions == 0 && CheckCompleted(data, "Failed partitions") ? $"[{OutputFormatter.Success.ToMarkup()}]✓[/]" : $"[{OutputFormatter.Danger.ToMarkup()}]✗[/]";
        var failedDetail = data.FailedPartitions == 0
            ? $"[{OutputFormatter.Success.ToMarkup()}]none[/]"
            : $"[{OutputFormatter.Danger.ToMarkup()}]{data.FailedPartitions} need attention[/]";
        table.AddRow(failedIcon, $"[{OutputFormatter.Accent.ToMarkup()}]Failed partitions[/]", CheckDetail(data, "Failed partitions", failedDetail, reasonsAbove: true));

        var recsIcon = data.PendingRecommendations == 0 ? $"[{OutputFormatter.Success.ToMarkup()}]✓[/]" : $"[{OutputFormatter.Warning.ToMarkup()}]▲[/]";
        if (!CheckCompleted(data, "Recommendations"))
        {
            recsIcon = $"[{OutputFormatter.Danger.ToMarkup()}]✗[/]";
        }

        var recsDetail = data.PendingRecommendations == 0
            ? $"[{OutputFormatter.Success.ToMarkup()}]none[/]"
            : $"[{OutputFormatter.Warning.ToMarkup()}]{data.PendingRecommendations} pending[/]";
        table.AddRow(recsIcon, $"[{OutputFormatter.Accent.ToMarkup()}]Recommendations[/]", CheckDetail(data, "Recommendations", recsDetail, reasonsAbove: true));

        var tailDetail = data.EventSequenceTail.HasValue
            ? $"{data.EventSequenceTail.Value:N0}"
            : $"[{OutputFormatter.Muted.ToMarkup()}]empty[/]";
        if (data.IsAggregate)
        {
            tailDetail = "per scope (see below)";
        }

        var tailIcon = CheckCompleted(data, "Event sequence") ? pendingIcon : unavailableIcon;
        table.AddRow(tailIcon, $"[{OutputFormatter.Accent.ToMarkup()}]Event sequence tail[/]", CheckDetail(data, "Event sequence", tailDetail, reasonsAbove: true));
        foreach (var finding in data.Findings.Take(maximumRows))
        {
            table.AddRow("!", $"{finding.EventStore.EscapeMarkup()}/{finding.Namespace.EscapeMarkup()}", $"{finding.Check.EscapeMarkup()}: {finding.Detail.EscapeMarkup()}");
        }

        if (data.Findings.Count > maximumRows)
        {
            table.AddRow("!", "Findings", WatchOverflowDetail(data.Findings.Count - maximumRows, "findings"));
        }

        foreach (var scope in data.Scopes.Where(_ => ShowScopeTails(data)).Take(maximumRows))
        {
            var scopeTailIcon = CheckCompleted(scope, "Event sequence") ? pendingIcon : unavailableIcon;
            table.AddRow(scopeTailIcon, $"{scope.EventStore.EscapeMarkup()}/{scope.Namespace.EscapeMarkup()}", $"tail: {ScopeTailDetail(scope)}");
        }

        if (ShowScopeTails(data) && data.Scopes.Count > maximumRows)
        {
            table.AddRow("·", "Event sequence tails", WatchOverflowDetail(data.Scopes.Count - maximumRows, "tails"));
        }

        return table;
    }
}
