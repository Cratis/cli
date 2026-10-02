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
        using var reserve = new TcpListener(IPAddress.Loopback, 0);
        reserve.Start();
        var port = ((IPEndPoint)reserve.LocalEndpoint).Port;
        reserve.Stop();

        var redirect = new Uri($"http://127.0.0.1:{port}/callback");
        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
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

    internal async Task<string> WaitForCallback(HttpListener listener, Uri redirect, string state, Uri issuer, CancellationToken cancellationToken)
    {
        while (true)
        {
            var context = await listener.GetContextAsync().WaitAsync(cancellationToken);
            try
            {
                if (context.Request.HttpMethod != "GET" || context.Request.Url is null ||
                    !IPAddress.IsLoopback(context.Request.LocalEndPoint.Address) || context.Request.Url.Host != "127.0.0.1")
                {
                    throw new DirectAuthError("Unexpected OAuth callback request.");
                }

                var code = DirectCallback.Validate(redirect, context.Request.Url, state, issuer);
                return await Complete(code, async () =>
                {
                    var message = Encoding.UTF8.GetBytes("Sign-in complete. You can close this tab.");
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    context.Response.ContentLength64 = message.Length;
                    await context.Response.OutputStream.WriteAsync(message, cancellationToken);
                });
            }
            catch (DirectAuthorizationDeclined declined)
            {
                // A fixed page only: the error parameters are never reflected back to the browser.
                throw await Decline(declined, async () =>
                {
                    var message = Encoding.UTF8.GetBytes("Sign-in was not completed. Return to the terminal for details.");
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/plain; charset=utf-8";
                    context.Response.ContentLength64 = message.Length;
                    await context.Response.OutputStream.WriteAsync(message, cancellationToken);
                });
            }
            catch (Exception ex) when (ex is DirectAuthError or UriFormatException)
            {
                context.Response.StatusCode = 400;

                // Do not reflect the rejected callback or its parameters in the response.
            }
            finally
            {
                Close(context.Response);
            }
        }
    }

    static bool IsDisconnect(Exception ex) => ex is IOException or HttpListenerException or ObjectDisposedException;

    static void Close(HttpListenerResponse response)
    {
        try
        {
            response.Close();
        }
        catch (Exception ex) when (IsDisconnect(ex))
        {
            // Closing a response to a disconnected browser must not replace the callback outcome.
        }
    }

    static async Task Open(string url, CancellationToken cancellationToken)
    {
        ProcessStartInfo start;
        if (OperatingSystem.IsMacOS())
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

        if (!OperatingSystem.IsWindows())
        {
            start.ArgumentList.Add(url);
        }

        using var process = Process.Start(start) ?? throw new InvalidOperationException("Browser process could not be started.");
        if (OperatingSystem.IsWindows())
        {
            return;
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
}
