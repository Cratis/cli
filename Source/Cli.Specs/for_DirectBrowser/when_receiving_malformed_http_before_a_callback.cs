// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Text;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectBrowser;

public class when_receiving_malformed_http_before_a_callback : Specification
{
    readonly List<string> _responses = [];
    string _code = null!;
    bool _bound;

    async Task Because()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var endpoint = (IPEndPoint)listener.LocalEndpoint;
        _bound = endpoint.Address.Equals(IPAddress.Loopback) && endpoint.Port != 0;
        var redirect = new Uri($"http://127.0.0.1:{endpoint.Port}/callback");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
#pragma warning disable CA2025 // The callback is canceled and awaited in finally before disposing its listener.
        var callback = new DirectBrowser().WaitForCallback(listener, redirect, "expected", new Uri("https://identity.example/"), deadline.Token);
#pragma warning restore CA2025
        try
        {
            const string path = "/callback?code=secret&state=expected&iss=https%3A%2F%2Fidentity.example%2F";
            var host = $"Host: 127.0.0.1:{endpoint.Port}";
            var invalid = new[]
            {
                $"POST {path} HTTP/1.1\r\n{host}\r\n\r\n",
                $"GET {path} HTTP/1.1\r\nHost: localhost:{endpoint.Port}\r\n\r\n",
                $"GET {path} HTTP/1.1\r\n{host}\r\n{host}\r\n\r\n",
                $"GET http://evil.example{path} HTTP/1.1\r\n{host}\r\n\r\n",
                $"GET {path} HTTP/1.1\r\n{host}\r\nTransfer-Encoding: chunked\r\n\r\n",
                $"GET {path} HTTP/1.1\r\n{host}\r\nMalformed\r\n\r\n",

                // Exhaust the server's exact header limit without leaving unread bytes that cause a TCP reset.
                $"GET {path} HTTP/1.1\r\n{host}\r\nX-Large: {new string('a', 16384)}"[..16384]
            };
            foreach (var request in invalid)
            {
                using var client = new TcpClient();
                await client.ConnectAsync(endpoint, deadline.Token);
                await using var stream = client.GetStream();
                await stream.WriteAsync(Encoding.ASCII.GetBytes(request), deadline.Token);
                using var reader = new StreamReader(stream);
                _responses.Add(await reader.ReadToEndAsync(deadline.Token));
            }

            using var http = new HttpClient(new HttpClientHandler { UseProxy = false, CheckCertificateRevocationList = true });
            using var response = await http.GetAsync($"{redirect}?code=good&state=expected&iss=https%3A%2F%2Fidentity.example%2F", deadline.Token);
            _code = await callback;
        }
        finally
        {
            await deadline.CancelAsync();
            await Catch.Exception(async () => await callback);
        }
    }

    [Fact] void should_keep_an_ephemeral_ipv4_loopback_socket_bound() => _bound.ShouldBeTrue();
    [Fact] void should_reject_non_get_wrong_host_duplicate_host_absolute_targets_and_unbounded_headers() => _responses.TrueForAll(response => response.StartsWith("HTTP/1.1 400", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_reflect_rejected_request_parameters() => _responses.TrueForAll(response => !response.Contains("secret", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_still_accept_a_valid_callback_on_the_bound_port() => _code.ShouldEqual("good");
}
