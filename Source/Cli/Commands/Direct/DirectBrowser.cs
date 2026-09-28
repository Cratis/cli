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
        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code", ["client_id"] = "cratis-cli", ["redirect_uri"] = redirect.AbsoluteUri,
            ["code_challenge"] = challenge.Challenge, ["code_challenge_method"] = "S256",
            ["state"] = challenge.State, ["scope"] = "direct:read direct:content.write direct:work",
            ["resource"] = target.Resource.AbsoluteUri
        };
        if (target.Tenant is not null)
        {
            query["tenant"] = target.Tenant;
        }

        var builder = new UriBuilder(endpoints.Authorization)
        {
            Query = string.Join('&', query.Select(pair => $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"))
        };
        var url = builder.Uri.AbsoluteUri;
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
            var context = await listener.GetContextAsync().WaitAsync(timeout.Token);
            string code;
            try
            {
                if (context.Request.HttpMethod != "GET" || context.Request.Url is null ||
                    !IPAddress.IsLoopback(context.Request.LocalEndPoint.Address) || context.Request.Url.Host != "127.0.0.1")
                {
                    throw new DirectAuthError("Unexpected OAuth callback request.");
                }

                code = DirectCallback.Validate(redirect, context.Request.Url, challenge.State, endpoints.Issuer);
                var message = Encoding.UTF8.GetBytes("Sign-in complete. You can close this tab.");
                context.Response.ContentType = "text/plain; charset=utf-8";
                context.Response.ContentLength64 = message.Length;
                await context.Response.OutputStream.WriteAsync(message, timeout.Token);
            }
            catch (DirectAuthError)
            {
                context.Response.StatusCode = 400;
                throw;
            }
            finally
            {
                context.Response.Close();
            }

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
