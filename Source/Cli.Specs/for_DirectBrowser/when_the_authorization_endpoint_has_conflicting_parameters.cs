// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectBrowser;

public class when_the_authorization_endpoint_has_conflicting_parameters : Specification
{
    Exception _error = null!;

    void Because()
    {
        var issuer = new Uri("https://identity.example/");
        var endpoints = new DirectEndpoints(issuer, new Uri(issuer, "authorize?%73tate=forged"), new Uri(issuer, "token"), new Uri(issuer, "revoke"));
        _error = Catch.Exception(() => new DirectBrowser().CreateAuthorizationUrl(endpoints, DirectTarget.Create("https://cratis.direct", null), new Uri("http://127.0.0.1:34123/callback"), DirectChallenge.Create()));
    }

    [Fact] void should_reject_ambiguous_oauth_parameters() => _error.ShouldBeOfExactType<DirectAuthError>();
}
