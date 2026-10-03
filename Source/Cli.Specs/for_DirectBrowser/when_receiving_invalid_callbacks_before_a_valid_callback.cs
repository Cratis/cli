// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectBrowser;

public class when_receiving_invalid_callbacks_before_a_valid_callback : Specification
{
    HttpStatusCode[] _rejections = null!;
    string[] _responses = null!;
    HttpStatusCode _accepted;
    string _code = null!;

    async Task Because()
    {
        using var reserve = new TcpListener(IPAddress.Loopback, 0);
        reserve.Start();
        var port = ((IPEndPoint)reserve.LocalEndpoint).Port;
        reserve.Stop();
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        listener.Start();
        using var http = new HttpClient(new HttpClientHandler { UseProxy = false, CheckCertificateRevocationList = true }) { Timeout = TimeSpan.FromSeconds(10) };
        var redirect = new Uri($"http://127.0.0.1:{port}/callback");
        var issuer = new Uri("https://identity.example/");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
#pragma warning disable CA2025 // The callback is awaited on success and canceled and awaited in finally on failure.
        var callback = new DirectBrowser().WaitForCallback(listener, redirect, "expected", issuer, deadline.Token);
#pragma warning restore CA2025
        try
        {
            _rejections = new HttpStatusCode[3];
            _responses = new string[3];
            var invalid = new[]
            {
                $"http://127.0.0.1:{port}/wrong?code=bad&state=expected&iss=https%3A%2F%2Fidentity.example%2F",
                $"http://127.0.0.1:{port}/callback?code=bad&state=wrong&iss=https%3A%2F%2Fidentity.example%2F",
                $"http://127.0.0.1:{port}/callback?code=bad&state=expected&iss=https%3A%2F%2Fevil.example%2F"
            };
            for (var index = 0; index < invalid.Length; index++)
            {
                using var response = await http.GetAsync(invalid[index], deadline.Token);
                _rejections[index] = response.StatusCode;
                _responses[index] = await response.Content.ReadAsStringAsync(deadline.Token);
            }

            using var valid = await http.GetAsync($"{redirect}?code=good&state=expected&iss=https%3A%2F%2Fidentity.example%2F", deadline.Token);
            _accepted = valid.StatusCode;
            _code = await callback;
        }
        finally
        {
            if (!callback.IsCompleted)
            {
                await deadline.CancelAsync();
                listener.Stop();
                try
                {
                    await callback;
                }
                catch (Exception ex) when (ex is OperationCanceledException or HttpListenerException)
                {
                    // The spec failed before completing the callback; finish its task before disposing the listener.
                }
            }

            listener.Close();
        }
    }

    [Fact] void should_reject_each_invalid_callback() => _rejections.ShouldContainOnly([HttpStatusCode.BadRequest, HttpStatusCode.BadRequest, HttpStatusCode.BadRequest]);
    [Fact] void should_not_reflect_callback_parameters() => _responses.All(response => !response.Contains("bad", StringComparison.Ordinal) && !response.Contains("evil.example", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_accept_the_later_valid_callback() => _accepted.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_return_only_the_valid_code() => _code.ShouldEqual("good");
}
