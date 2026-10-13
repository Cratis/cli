// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectBrowser;

public class when_creating_an_authorization_url : Specification
{
    Uri _url = null!;

    void Because()
    {
        var issuer = new Uri("https://identity.example/");
        var endpoints = new DirectEndpoints(issuer, new Uri(issuer, "connect/authorize"), new Uri(issuer, "connect/token"), new Uri(issuer, "connect/revocation"));
        var target = DirectTarget.Create("https://cratis.direct", "team");
        var challenge = DirectChallenge.Create();
        _url = new DirectBrowser().CreateAuthorizationUrl(endpoints, target, new Uri("http://127.0.0.1:34123/callback"), challenge);
    }

    [Fact] void should_use_the_discovered_authorization_endpoint() => _url.GetLeftPart(UriPartial.Path).ShouldEqual("https://identity.example/connect/authorize");
    [Fact] void should_request_the_public_client_without_a_secret() => _url.Query.ShouldContain("client_id=cratis-cli&redirect_uri=");
    [Fact] void should_request_exactly_the_ipv4_loopback_callback() => _url.Query.ShouldContain("redirect_uri=" + Uri.EscapeDataString("http://127.0.0.1:34123/callback"));
    [Fact] void should_request_pkce_s256() => _url.Query.ShouldContain("code_challenge_method=S256");
    [Fact] void should_request_all_direct_scopes_and_offline_access() => _url.Query.ShouldContain("scope=" + Uri.EscapeDataString("direct:read direct:content.write direct:work offline_access"));
    [Fact] void should_bind_the_direct_mcp_resource() => _url.Query.ShouldContain("resource=" + Uri.EscapeDataString("https://cratis.direct/mcp"));
    [Fact] void should_send_the_tenant_hint() => _url.Query.ShouldContain("tenant=team");
    [Fact] void should_not_send_a_client_secret() => _url.Query.Contains("client_secret", StringComparison.Ordinal).ShouldBeFalse();
}
