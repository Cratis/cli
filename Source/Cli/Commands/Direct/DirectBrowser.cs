// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cratis.Cli.Commands.Direct;

/// <summary>External-browser authorization code + PKCE with an exact RFC 8252 IPv4 loopback redirect.</summary>
internal sealed class DirectBrowser
{
    /// <summary>Shows the fixed page for a declined login and returns the error to report, even if the browser tab is gone.</summary>
    /// <param name="declined">The validated OAuth error.</param>
    /// <param name="showPage">Writes the fixed page to the browser.</param>
    /// <returns>The error carrying the specific guidance for the OAuth error.</returns>
    internal static async Task<DirectAuthError> Decline(DirectAuthorizationDeclined declined, Func<Task> showPage)
    {
        try
        {
            await showPage();
        }
        catch (Exception ex) when (IsDisconnect(ex))
        {
            // The browser closed the connection; the guidance still belongs in the terminal.
        }

        return new DirectAuthError(declined.Guidance);
    }

    internal static async Task<string> Complete(string code, Func<Task> showPage)
    {
        try
        {
            await showPage();
        }
        catch (Exception ex) when (IsDisconnect(ex))
        {
            // The validated authorization code still belongs to the CLI if the browser disconnects.
        }

        return code;
    }

    internal async Task<(string Code, string Verifier, Uri Redirect)> Authorize(DirectEndpoints endpoints, DirectTarget target, CancellationToken cancellationToken)
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var redirect = new Uri($"http://127.0.0.1:{port}/callback");
        var challenge = DirectChallenge.Create();
        var url = CreateAuthorizationUrl(endpoints, target, redirect, challenge).AbsoluteUri;
        try
        {
            await Open(url, cancellationToken);
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            await Console.Error.WriteLineAsync($"Could not open the browser. Visit this URL to sign in: {url}");
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(3));
        try
        {
            var code = await WaitForCallback(listener, redirect, challenge.State, endpoints.Issuer, timeout.Token);
            return (code, challenge.Verifier, redirect);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new DirectAuthError("Timed out waiting for the browser to finish Direct login.");
        }
        finally
        {
            listener.Stop();
        }
    }

    internal Uri CreateAuthorizationUrl(DirectEndpoints endpoints, DirectTarget target, Uri redirect, DirectChallenge challenge)
    {
        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code", ["client_id"] = "cratis-cli", ["redirect_uri"] = redirect.AbsoluteUri,
            ["code_challenge"] = challenge.Challenge, ["code_challenge_method"] = "S256",
            ["state"] = challenge.State, ["scope"] = "direct:read direct:content.write direct:work offline_access",
            ["resource"] = target.Resource.AbsoluteUri
        };
        if (target.Tenant is not null)
        {
            query["tenant"] = target.Tenant;
        }

        var existing = endpoints.Authorization.Query.TrimStart('?');
        if (existing.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => Uri.UnescapeDataString(pair.Split('=')[0].Replace('+', ' ')))
            .Any(name => query.ContainsKey(name) || name == "tenant"))
        {
            throw new DirectAuthError("Authorization endpoint query conflicts with Direct login parameters.");
        }

        var parameters = string.Join('&', query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        var builder = new UriBuilder(endpoints.Authorization)
        {
            Query = existing.Length == 0 ? parameters : $"{existing}&{parameters}"
        };
        return builder.Uri;
    }

    internal async Task<string> WaitForCallback(TcpListener listener, Uri redirect, string state, Uri issuer, CancellationToken cancellationToken)
    {
        while (true)
        {
            using var client = await listener.AcceptTcpClientAsync(cancellationToken);
            await using var stream = client.GetStream();
            using var requestDeadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            requestDeadline.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                var callback = await ReadRequest(stream, redirect, requestDeadline.Token);
                var code = DirectCallback.Validate(redirect, callback, state, issuer);
                return await Complete(code, () => Respond(stream, 200, "Sign-in complete. You can close this tab.", requestDeadline.Token));
            }
            catch (DirectAuthorizationDeclined declined)
            {
                // A fixed page only: the error parameters are never reflected back to the browser.
                throw await Decline(declined, () => Respond(stream, 200, "Sign-in was not completed. Return to the terminal for details.", requestDeadline.Token));
            }
            catch (Exception ex) when (ex is DirectAuthError or UriFormatException)
            {
                try
                {
                    await Respond(stream, 400, string.Empty, requestDeadline.Token);
                }
                catch (Exception disconnected) when (IsDisconnect(disconnected) || (disconnected is OperationCanceledException && !cancellationToken.IsCancellationRequested))
                {
                    // A rejected request does not own the login attempt.
                }
            }
            catch (Exception ex) when (IsDisconnect(ex) || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                // A disconnected or stalled unvalidated request must not end the callback wait.
            }
        }
    }

    internal async Task Open(string url, CancellationToken cancellationToken, Func<ProcessStartInfo, Process?>? launch = null, bool? windows = null)
    {
        var isWindows = windows ?? OperatingSystem.IsWindows();
        ProcessStartInfo start;
        if (isWindows)
        {
            start = new ProcessStartInfo(url) { UseShellExecute = true };
        }
        else if (OperatingSystem.IsMacOS())
        {
            start = new ProcessStartInfo("open");
        }
        else if (OperatingSystem.IsLinux())
        {
            start = new ProcessStartInfo("xdg-open");
        }
        else
        {
            start = new ProcessStartInfo(url) { UseShellExecute = true };
        }

        if (!isWindows)
        {
            start.ArgumentList.Add(url);
        }

        using var process = (launch ?? Process.Start)(start);
        if (isWindows)
        {
            // Shell execution can hand the URL to an existing browser without returning a process.
            return;
        }

        if (process is null)
        {
            throw new InvalidOperationException("Browser process could not be started.");
        }

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(2));
        try
        {
            await process.WaitForExitAsync(deadline.Token);
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException("Browser launcher reported a failure.");
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Some launchers stay attached to the browser. Once launched, wait for the OAuth callback instead.
        }
    }

    static bool IsDisconnect(Exception ex) => ex is IOException or HttpListenerException or ObjectDisposedException;

    static async Task<Uri> ReadRequest(NetworkStream stream, Uri redirect, CancellationToken cancellationToken)
    {
        var header = new StringBuilder();
        var buffer = new byte[1];
        while (header.Length < 16384)
        {
            if (await stream.ReadAsync(buffer, cancellationToken) == 0 || buffer[0] > 127)
            {
                throw new DirectAuthError("Unexpected OAuth callback request.");
            }

            header.Append((char)buffer[0]);
            if (header.Length >= 4 && header.ToString(header.Length - 4, 4) == "\r\n\r\n")
            {
                var lines = header.ToString().Split("\r\n", StringSplitOptions.None);
                var request = lines[0].Split(' ');
                var hosts = lines.Skip(1).Where(line => line.StartsWith("Host:", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (request.Length != 3 || request[0] != "GET" || (request[2] != "HTTP/1.1" && request[2] != "HTTP/1.0") ||
                    !request[1].StartsWith('/') || request[1].StartsWith("//", StringComparison.Ordinal) || request[1].Contains('#') ||
                    hosts.Length != 1 || !string.Equals(hosts[0][5..].Trim(), redirect.Authority, StringComparison.Ordinal) ||
                    lines.Skip(1).Any(line => line.Length != 0 && (line[0] is ' ' or '\t' || !line.Contains(':') ||
                        line.StartsWith("Transfer-Encoding:", StringComparison.OrdinalIgnoreCase) || line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))))
                {
                    throw new DirectAuthError("Unexpected OAuth callback request.");
                }

                return new Uri(redirect.GetLeftPart(UriPartial.Authority) + request[1]);
            }
        }

        throw new DirectAuthError("Unexpected OAuth callback request.");
    }

    static async Task Respond(NetworkStream stream, int status, string message, CancellationToken cancellationToken)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var header = Encoding.ASCII.GetBytes($"HTTP/1.1 {status} {(status == 200 ? "OK" : "Bad Request")}\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(body, cancellationToken);
    }
}
