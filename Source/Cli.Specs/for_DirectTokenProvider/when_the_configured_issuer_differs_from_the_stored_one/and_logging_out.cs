// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_the_configured_issuer_differs_from_the_stored_one;

public class and_logging_out : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    Exception _error = null!;

    void Establish() => _server.Store(_target, "stored-refresh");

    async Task Because() => _error = await Catch.Exception(() => _server.Provider("https://attacker.example/").Revoke(_target, CancellationToken.None));

    [Fact] void should_refuse_to_revoke() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_not_send_any_request() => _server.Requests.ShouldBeEmpty();
    [Fact] void should_retain_the_stored_credential() => _server.Stored.ContainsKey(_target.Key).ShouldBeTrue();

    void Destroy() => _server.Dispose();
}
