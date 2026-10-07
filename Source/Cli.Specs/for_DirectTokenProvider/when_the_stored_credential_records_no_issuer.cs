// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider;

public class when_the_stored_credential_records_no_issuer : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    Exception _error = null!;

    void Establish() => _server.Store(_target, "stored-refresh", issuer: null, expiresAt: DateTimeOffset.UtcNow.AddMinutes(-1));

    async Task Because() => _error = await Catch.Exception(() => _server.Provider().GetAccessToken(_target, new Uri("https://identity.example/"), CancellationToken.None));

    [Fact] void should_refuse_to_refresh() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_ask_for_a_new_login() => _error.Message.ShouldContain("cratis direct login");
    [Fact] void should_not_send_any_request() => _server.Requests.ShouldBeEmpty();

    void Destroy() => _server.Dispose();
}
