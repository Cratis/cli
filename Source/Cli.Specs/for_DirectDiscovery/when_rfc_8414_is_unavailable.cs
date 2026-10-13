// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectDiscovery;

public class when_rfc_8414_is_unavailable : Specification
{
    DirectEndpoints _endpoints = null!;
    string[] _requests = null!;

    async Task Because()
    {
        var requests = new List<string>();
        using var http = new HttpClient(new Handler(request =>
        {
            requests.Add(request.RequestUri!.AbsolutePath);
            return request.RequestUri.AbsolutePath switch
            {
                "/.well-known/oauth-protected-resource/mcp" => Json("{\"resource\":\"https://direct.example/mcp\",\"authorization_servers\":[\"https://identity.example/tenant\"]}"),
                "/.well-known/oauth-authorization-server/tenant" => new HttpResponseMessage(HttpStatusCode.NotFound),
                "/tenant/.well-known/openid-configuration" => Json("{\"issuer\":\"https://identity.example/tenant\",\"authorization_endpoint\":\"https://identity.example/authorize\",\"token_endpoint\":\"https://identity.example/token\",\"revocation_endpoint\":\"https://identity.example/revoke\"}"),
                _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            };
        }));
        _endpoints = await new DirectDiscovery(http).Discover(DirectTarget.Create("https://direct.example", "team"), null, CancellationToken.None);
        _requests = [.. requests];
    }

    [Fact] void should_choose_the_discovered_issuer() => _endpoints.Issuer.AbsoluteUri.ShouldEqual("https://identity.example/tenant");
    [Fact] void should_fall_back_only_after_a_404() => _requests.ShouldContain("/tenant/.well-known/openid-configuration");
    [Fact] void should_use_the_rfc_8414_path_for_path_based_issuers() => _requests.ShouldContain("/.well-known/oauth-authorization-server/tenant");

    static HttpResponseMessage Json(string json) => new(HttpStatusCode.OK) { Content = new StringContent(json) };

    sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(answer(request));
    }
}
