// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;

namespace Cratis.Cli.Commands.Arc;

/// <summary>
/// Base class for all CLI commands that interact with a Cratis Arc application over HTTP.
/// </summary>
/// <typeparam name="TSettings">The settings type for this command.</typeparam>
public abstract class ArcCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : ArcSettings
{
    /// <inheritdoc/>
    protected sealed override async Task<int> ExecuteAsync(CommandContext context, TSettings settings, CancellationToken cancellationToken)
    {
        var format = settings.ResolveOutputFormat();

        try
        {
            using var handler = CreateHttpHandler();
            using var httpClient = new HttpClient(handler);
            return await ExecuteCommandAsync(httpClient, settings, format, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException || ex.StatusCode is null)
        {
            OutputFormatter.WriteError(format, ArcDefaults.CannotConnectMessage, BuildConnectionHint(settings), ExitCodes.ConnectionErrorCode);
            return ExitCodes.ConnectionError;
        }
        catch (HttpRequestException ex)
        {
            OutputFormatter.WriteError(format, $"HTTP error: {ex.Message}", BuildConnectionHint(settings), ExitCodes.ServerErrorCode);
            return ExitCodes.ServerError;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OutputFormatter.WriteError(format, ex.Message, errorCode: ExitCodes.ServerErrorCode);
            return ExitCodes.ServerError;
        }
    }

    /// <summary>
    /// Executes the command logic using the provided <see cref="HttpClient"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client configured to reach the Arc application.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="format">The resolved output format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The exit code.</returns>
    protected abstract Task<int> ExecuteCommandAsync(HttpClient httpClient, TSettings settings, string format, CancellationToken cancellationToken);

#pragma warning disable MA0039 // TLS validation is intentionally skipped for local development Arc applications
    static HttpClientHandler CreateHttpHandler() =>
        new() { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
#pragma warning restore MA0039

    static string BuildConnectionHint(ArcSettings settings) =>
        $"Verify the Arc application is running at {settings.ResolveUrl()}\n" +
        "Use --url to specify a different URL, or set the ARC_URL environment variable.";
}
