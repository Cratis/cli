// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectDiscovery;

public class when_resource_metadata_is_missing_and_issuer_is_explicit : Specification
{
    DirectEndpoints _endpoints = null!;

    async Task Because()
    {
        using var http = new HttpClient(new Handler(request => request.RequestUri!.AbsolutePath switch
        {
            "/.well-known/oauth-protected-resource/mcp" => new HttpResponseMessage(HttpStatusCode.NotFound),
            "/.well-known/oauth-authorization-server" => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"issuer\":\"https://identity.example\",\"authorization_endpoint\":\"https://identity.example/authorize\",\"token_endpoint\":\"https://identity.example/token\",\"revocation_endpoint\":\"https://identity.example/revoke\"}")
            },
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        }));
        _endpoints = await new DirectDiscovery(http).Discover(DirectTarget.Create("https://direct.example", null), "https://identity.example", CancellationToken.None);
    }

    [Fact] void should_keep_the_exact_issuer_string_without_a_trailing_slash() => _endpoints.Issuer.OriginalString.ShouldEqual("https://identity.example");

    sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(answer(request));
    }
}
