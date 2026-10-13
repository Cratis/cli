// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectDiscovery;

public class when_a_remembered_issuer_is_no_longer_advertised : Specification
{
    DirectEndpoints _endpoints = null!;
    Exception? _explicitError;

    async Task Because()
    {
        using var http = new HttpClient(new Handler());
        var discovery = new DirectDiscovery(http);
        var target = DirectTarget.Create("https://direct.example", null);
        _endpoints = await discovery.Discover(target, null, CancellationToken.None, "https://old.example/");
        _explicitError = await Catch.Exception(() => discovery.Discover(target, "https://old.example/", CancellationToken.None));
    }

    [Fact] void should_use_the_currently_advertised_issuer() => _endpoints.Issuer.OriginalString.ShouldEqual("https://identity.example/");
    [Fact] void should_still_reject_an_explicit_unlisted_issuer() => _explicitError.ShouldBeOfExactType<DirectAuthError>();

    sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(request.RequestUri!.AbsolutePath.Contains("oauth-protected-resource", StringComparison.Ordinal)
                ? "{\"resource\":\"https://direct.example/mcp\",\"authorization_servers\":[\"https://identity.example/\"]}"
                : "{\"issuer\":\"https://identity.example/\",\"authorization_endpoint\":\"https://identity.example/authorize\",\"token_endpoint\":\"https://identity.example/token\",\"revocation_endpoint\":\"https://identity.example/revoke\"}")
        });
    }
}
