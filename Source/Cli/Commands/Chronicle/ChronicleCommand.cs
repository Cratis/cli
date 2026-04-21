// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Cratis.Cli.Commands.Version;
using Grpc.Core;

namespace Cratis.Cli.Commands.Chronicle;

/// <summary>
/// Base class for all CLI commands that need a Chronicle connection.
/// </summary>
/// <typeparam name="TSettings">The settings type for this command.</typeparam>
public abstract partial class ChronicleCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : ChronicleSettings
{
    /// <inheritdoc/>
    protected sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        if (settings.Debug)
        {
            WriteDebugInfo(settings);
        }

        // Start update check in the background — never blocks the command.
        using var updateCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        updateCts.CancelAfter(TimeSpan.FromSeconds(5));
        var updateCheckTask = UpdateChecker.CheckForUpdate(VersionCommand.GetCliVersion(), updateCts.Token);

        var tokenRefreshAttempted = false;
        while (true)
        {
            try
            {
                var connectionString = new ChronicleConnectionString(settings.ResolveConnectionString());
                var managementPort = settings.ResolveManagementPort();
                using var client = await CliChronicleConnection.Connect(connectionString, managementPort, cancellationToken);

                int exitCode;
                var sw = settings.Debug ? Stopwatch.StartNew() : null;

                if (format is OutputFormats.Table)
                {
                    exitCode = await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(new Style(OutputFormatter.Accent))
                        .StartAsync("Connecting...", async _ =>
                            await ExecuteCommandAsync(client.Services, settings, format));
                }
                else
                {
                    exitCode = await ExecuteCommandAsync(client.Services, settings, format);
                }

                if (sw is not null)
                {
                    sw.Stop();
                    await Console.Error.WriteLineAsync($"[debug] command completed in {sw.ElapsedMilliseconds}ms, exit code {exitCode}");
                }

                await ShowUpdateHint(updateCheckTask, format);
                return exitCode;
            }
            catch (RpcException ex) when (!tokenRefreshAttempted && IsHttpUnauthorized(ex))
            {
                // Cached token was rejected — clear it and retry once with a fresh token.
                tokenRefreshAttempted = true;
                var config = CliConfiguration.Load();
                var cs = new ChronicleConnectionString(settings.ResolveConnectionString());
                CliChronicleConnection.ClearTokenCache(config.ActiveContextName, cs.Username ?? string.Empty);
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable || IsNetworkException(ex.InnerException))
            {
                OutputFormatter.WriteError(format, CliDefaults.CannotConnectMessage, $"Verify the server is running and reachable. Connection: {settings.ResolveConnectionString()}", ExitCodes.ConnectionErrorCode);
                return ExitCodes.ConnectionError;
            }
            catch (RpcException ex) when (ex.Status.Detail.Contains("disposed", StringComparison.OrdinalIgnoreCase))
            {
                OutputFormatter.WriteError(format, "Server error", $"{ex.Status.Detail}", ExitCodes.ServerErrorCode);
                return ExitCodes.ServerError;
            }
            catch (RpcException ex)
            {
                OutputFormatter.WriteError(format, $"Server error: {ex.Status.Detail}", errorCode: ExitCodes.ServerErrorCode);
                return ExitCodes.ServerError;
            }
            catch (ObjectDisposedException)
            {
                OutputFormatter.WriteError(format, CliDefaults.CannotConnectMessage, $"Verify the server is running and reachable. Connection: {settings.ResolveConnectionString()}", ExitCodes.ConnectionErrorCode);
                return ExitCodes.ConnectionError;
            }
            catch (HttpRequestException ex)
            {
                OutputFormatter.WriteError(format, ex.InnerException is SocketException ? $"Connection refused ({settings.ResolveConnectionString()})" : ex.Message, errorCode: ExitCodes.ConnectionErrorCode);
                return ExitCodes.ConnectionError;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                OutputFormatter.WriteError(format, ex.Message, errorCode: ExitCodes.ServerErrorCode);
                return ExitCodes.ServerError;
            }
        }
    }

    /// <summary>
    /// Executes the command logic with gRPC services.
    /// </summary>
    /// <param name="services">The gRPC service proxies.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="format">The resolved output format.</param>
    /// <returns>The exit code.</returns>
    protected abstract Task<int> ExecuteCommandAsync(IServices services, TSettings settings, string format);

    static void WriteDebugInfo(ChronicleSettings settings)
    {
        var configPath = CliConfiguration.GetConfigPath();
        var config = CliConfiguration.Load();
        var connectionString = settings.ResolveConnectionString();
        var managementPort = settings.ResolveManagementPort();

        Console.Error.WriteLine($"[debug] config:          {configPath}");
        Console.Error.WriteLine($"[debug] context:         {config.ActiveContextName}");
        Console.Error.WriteLine($"[debug] server:          {RedactConnectionString(connectionString)}");
        Console.Error.WriteLine($"[debug] management-port: {managementPort}");
        Console.Error.WriteLine($"[debug] output:          {settings.ResolveOutputFormat()}");

        if (settings is EventStoreSettings ess)
        {
            Console.Error.WriteLine($"[debug] event-store:     {ess.ResolveEventStore()}");
            Console.Error.WriteLine($"[debug] namespace:       {ess.ResolveNamespace()}");
        }
    }

    [System.Text.RegularExpressions.GeneratedRegex("://(?<user>[^:@/]+):[^@/]+@", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex ConnectionStringCredentialsRegex();

    static string RedactConnectionString(string connectionString) =>
        ConnectionStringCredentialsRegex().Replace(connectionString, "://${user}:***@");

    static bool IsHttpUnauthorized(RpcException ex) =>
        ex.Status.Detail.Contains("HTTP status code: 401", StringComparison.Ordinal);

    static bool IsNetworkException(Exception? ex)
    {
        while (ex is not null)
        {
            if (ex is HttpRequestException or SocketException)
            {
                return true;
            }

            ex = ex.InnerException;
        }

        return false;
    }

    static async Task ShowUpdateHint(Task<string?> updateCheckTask, string format)
    {
        try
        {
            if (!updateCheckTask.IsCompleted)
            {
                return;
            }

            var latestVersion = await updateCheckTask;
            if (latestVersion is null)
            {
                return;
            }

            if (format is OutputFormats.Table)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"  [{OutputFormatter.Warning.ToMarkup()}]\u2191 Update available:[/] {latestVersion.EscapeMarkup()} \u2014 run [bold]cratis update[/] to upgrade");
            }
        }
        catch
        {
            // Update check failures are non-critical.
        }
    }
}
