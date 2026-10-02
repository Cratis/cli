// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectBrowser;

public class when_creating_an_authorization_url_with_an_existing_query : Specification
{
    Uri _url = null!;

    void Because()
    {
        var issuer = new Uri("https://identity.example/");
        var endpoints = new DirectEndpoints(issuer, new Uri(issuer, "authorize?policy=direct&route=a%2Fb"), new Uri(issuer, "token"), new Uri(issuer, "revoke"));
        _url = new DirectBrowser().CreateAuthorizationUrl(endpoints, DirectTarget.Create("https://cratis.direct", null), new Uri("http://127.0.0.1:34123/callback"), DirectChallenge.Create());
    }

    [Fact] void should_preserve_the_endpoint_query() => _url.Query.ShouldContain("?policy=direct&route=a%2Fb&response_type=code");
    [Fact] void should_add_the_public_client() => _url.Query.ShouldContain("client_id=cratis-cli");
}
