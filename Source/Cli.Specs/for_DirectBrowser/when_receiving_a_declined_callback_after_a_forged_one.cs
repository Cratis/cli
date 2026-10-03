// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectBrowser;

public class when_receiving_a_declined_callback_after_a_forged_one : Specification
{
    HttpStatusCode _forged;
    bool _waitingAfterForged;
    HttpStatusCode _declined;
    string _page = null!;
    Exception _error = null!;
    TimeSpan _elapsed;

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
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var stopwatch = Stopwatch.StartNew();
#pragma warning disable CA2025 // The callback is awaited before the listener is closed.
        var callback = new DirectBrowser().WaitForCallback(listener, redirect, "expected", new Uri("https://identity.example/"), deadline.Token);
#pragma warning restore CA2025
        try
        {
            using (var forged = await http.GetAsync($"{redirect}?error=access_denied&state=forged&iss=https%3A%2F%2Fidentity.example%2F", deadline.Token))
            {
                _forged = forged.StatusCode;
            }

            _waitingAfterForged = !callback.IsCompleted;
            using var declined = await http.GetAsync($"{redirect}?error=access_denied&error_description=%3Cscript%3E&state=expected&iss=https%3A%2F%2Fidentity.example%2F", deadline.Token);
            _declined = declined.StatusCode;
            _page = await declined.Content.ReadAsStringAsync(deadline.Token);
            _error = await Catch.Exception(async () => await callback);
            _elapsed = stopwatch.Elapsed;
        }
        finally
        {
            if (!callback.IsCompleted)
            {
                await deadline.CancelAsync();
                listener.Stop();
                await Catch.Exception(async () => await callback);
            }

            listener.Close();
        }
    }

    [Fact] void should_reject_the_forged_callback() => _forged.ShouldEqual(HttpStatusCode.BadRequest);
    [Fact] void should_keep_waiting_after_the_forged_callback() => _waitingAfterForged.ShouldBeTrue();
    [Fact] void should_answer_the_declined_callback() => _declined.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_answer_with_a_fixed_page() => _page.ShouldEqual("Sign-in was not completed. Return to the terminal for details.");
    [Fact] void should_end_the_wait_with_an_authentication_error() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_report_the_error_code() => _error.Message.ShouldContain("(access_denied)");
    [Fact] void should_not_report_the_error_description() => _error.Message.ShouldNotContain("script");
    [Fact] void should_end_the_wait_immediately() => (_elapsed < TimeSpan.FromSeconds(10)).ShouldBeTrue();
}
