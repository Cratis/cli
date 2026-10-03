// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider;

public class when_saving_tokens : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    DirectTokens _stored = null!;

    async Task Because()
    {
        await _server.Provider().Save(_target, new DirectTokens("access", "refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), CancellationToken.None);
        _stored = JsonSerializer.Deserialize<DirectTokens>(_server.Stored[_target.Key])!;
    }

    [Fact] void should_record_the_issuer_with_the_secret() => _stored.Issuer.ShouldEqual("https://identity.example/");

    void Destroy() => _server.Dispose();
}
