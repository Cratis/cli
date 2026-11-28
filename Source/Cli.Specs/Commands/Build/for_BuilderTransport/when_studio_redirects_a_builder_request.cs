// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Cratis.Cli.Commands.Build.for_BuilderTransport;

public class when_studio_redirects_a_builder_request : Specification
{
    [Theory]
    [InlineData(307, true)]
    [InlineData(308, false)]
    public async Task should_never_contact_the_redirect_destination(int status, bool registration)
    {
        using var origin = new TcpListener(IPAddress.Loopback, 0);
        using var destination = new TcpListener(IPAddress.Loopback, 0);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        origin.Start();
        destination.Start();
        var client = BuilderTransport.Connect($"http://{origin.LocalEndpoint}", "org");
        client.Authenticate("id", "synthetic");
        Task request = registration ? client.Register("name", "machine", "synthetic", timeout.Token) : client.Connect(new(), timeout.Token);
        using var peer = await origin.AcceptTcpClientAsync(timeout.Token);
        await using var stream = peer.GetStream();
        using var reader = new StreamReader(stream, leaveOpen: true);
        while (await reader.ReadLineAsync(timeout.Token) is { Length: > 0 }) { }
        await stream.WriteAsync(Encoding.ASCII.GetBytes($"HTTP/1.1 {status} Redirect\r\nLocation: http://{destination.LocalEndpoint}/next\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"), timeout.Token);
        var error = await Catch.Exception(async () => await request);
        ((StudioBuilderRefused)error).Status.ShouldEqual((HttpStatusCode)status);
        destination.Pending().ShouldBeFalse();
    }
}
