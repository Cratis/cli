// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_the_configured_issuer_differs_from_the_stored_one;

public class and_the_access_token_expired : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    Exception _error = null!;

    void Establish() => _server.Store(_target, "stored-refresh", expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

    async Task Because() => _error = await Catch.Exception(() => _server.Provider("https://attacker.example/").GetAccessToken(_target, new Uri("https://attacker.example/"), CancellationToken.None));

    [Fact] void should_refuse_to_refresh() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_explain_the_issuer_mismatch() => _error.Message.ShouldContain("differs from the one that issued");
    [Fact] void should_not_send_any_request() => _server.Requests.ShouldBeEmpty();
    [Fact] void should_keep_the_stored_credential() => _server.Stored.ContainsKey(_target.Key).ShouldBeTrue();

    void Destroy() => _server.Dispose();
}
