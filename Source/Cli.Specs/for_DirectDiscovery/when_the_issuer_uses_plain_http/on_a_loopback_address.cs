// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectDiscovery.when_the_issuer_uses_plain_http;

public class on_a_loopback_address : Specification
{
    Uri _issuer = null!;

    void Because() => _issuer = DirectDiscovery.ValidateIssuer("http://127.0.0.1:5100/");

    [Fact] void should_accept_the_issuer() => _issuer.OriginalString.ShouldEqual("http://127.0.0.1:5100/");
}
