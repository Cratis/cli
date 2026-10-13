// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectDiscovery;

public class when_discovering_the_identity_root_issuer : Specification
{
    string _metadataPath = null!;
    DirectEndpoints _endpoints = null!;

    async Task Because()
    {
        using var http = new HttpClient(new Handler(request =>
        {
            _metadataPath = request.RequestUri!.AbsolutePath;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"issuer\":\"https://identity.example/\",\"authorization_endpoint\":\"https://identity.example/connect/authorize\",\"token_endpoint\":\"https://identity.example/connect/token\",\"revocation_endpoint\":\"https://identity.example/connect/revocation\"}")
            };
        }));
        _endpoints = await new DirectDiscovery(http).DiscoverIssuer(new Uri("https://identity.example/"), CancellationToken.None);
    }

    [Fact] void should_request_the_root_rfc_8414_metadata() => _metadataPath.ShouldEqual("/.well-known/oauth-authorization-server");
    [Fact] void should_preserve_the_exact_issuer() => _endpoints.Issuer.OriginalString.ShouldEqual("https://identity.example/");
    [Fact] void should_take_the_discovered_token_endpoint() => _endpoints.Token.AbsoluteUri.ShouldEqual("https://identity.example/connect/token");

    sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(answer(request));
    }
}
