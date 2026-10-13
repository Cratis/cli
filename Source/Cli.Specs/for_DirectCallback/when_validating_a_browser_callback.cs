// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCallback;

public class when_validating_a_browser_callback : Specification
{
    readonly Uri _redirect = new("http://127.0.0.1:34123/callback");
    readonly Uri _issuer = new("https://identity.example.com/");
    string _code = null!;

    void Because() => _code = DirectCallback.Validate(_redirect, new Uri("http://127.0.0.1:34123/callback?code=one&state=random&iss=https%3A%2F%2Fidentity.example.com%2F"), "random", _issuer);

    [Fact] void should_return_the_authorization_code() => _code.ShouldEqual("one");
}
