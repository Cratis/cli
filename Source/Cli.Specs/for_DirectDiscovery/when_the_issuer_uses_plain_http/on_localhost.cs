// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectDiscovery.when_the_issuer_uses_plain_http;

public class on_localhost : Specification
{
    DirectEndpoints _endpoints = null!;

    async Task Because()
    {
        using var http = new HttpClient(new Handler(request => request.RequestUri!.AbsoluteUri switch
        {
            "https://direct.example/.well-known/oauth-protected-resource/mcp" => new HttpResponseMessage(HttpStatusCode.NotFound),
            "http://localhost:5100/.well-known/oauth-authorization-server" => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"issuer\":\"http://localhost:5100/\",\"authorization_endpoint\":\"http://localhost:5100/connect/authorize\",\"token_endpoint\":\"http://localhost:5100/connect/token\",\"revocation_endpoint\":\"http://localhost:5100/connect/revoke\"}")
            },
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        }));
        _endpoints = await new DirectDiscovery(http).Discover(DirectTarget.Create("https://direct.example", null), "http://localhost:5100/", CancellationToken.None);
    }

    [Fact] void should_accept_the_issuer() => _endpoints.Issuer.OriginalString.ShouldEqual("http://localhost:5100/");
    [Fact] void should_accept_the_loopback_token_endpoint() => _endpoints.Token.AbsoluteUri.ShouldEqual("http://localhost:5100/connect/token");

    sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(answer(request));
    }
}
