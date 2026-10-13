// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectDiscovery;

public class when_resource_metadata_is_missing_and_an_issuer_is_remembered : Specification
{
    DirectEndpoints _endpoints = null!;

    async Task Because()
    {
        using var http = new HttpClient(new Handler());
        _endpoints = await new DirectDiscovery(http).Discover(DirectTarget.Create("https://direct.example", null), null, CancellationToken.None, "https://identity.example/");
    }

    [Fact] void should_use_the_remembered_issuer_only_as_a_fallback() => _endpoints.Issuer.OriginalString.ShouldEqual("https://identity.example/");

    sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(request.RequestUri!.AbsolutePath.Contains("oauth-protected-resource", StringComparison.Ordinal)
            ? new HttpResponseMessage(HttpStatusCode.NotFound)
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"issuer\":\"https://identity.example/\",\"authorization_endpoint\":\"https://identity.example/authorize\",\"token_endpoint\":\"https://identity.example/token\",\"revocation_endpoint\":\"https://identity.example/revoke\"}")
            });
    }
}
