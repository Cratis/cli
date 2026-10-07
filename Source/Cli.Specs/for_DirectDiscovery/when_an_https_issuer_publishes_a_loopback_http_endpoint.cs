// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectDiscovery;

public class when_an_https_issuer_publishes_a_loopback_http_endpoint : Specification
{
    Exception _error = null!;

    async Task Because()
    {
        using var http = new HttpClient(new Handler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"issuer\":\"https://identity.example/\",\"authorization_endpoint\":\"https://identity.example/authorize\",\"token_endpoint\":\"http://localhost:5100/token\",\"revocation_endpoint\":\"https://identity.example/revoke\"}")
        }));
        _error = await Catch.Exception(() => new DirectDiscovery(http).DiscoverIssuer(new Uri("https://identity.example/"), CancellationToken.None));
    }

    [Fact] void should_refuse_the_endpoint() => _error.Message.ShouldContain("token_endpoint");

    sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(answer(request));
    }
}
