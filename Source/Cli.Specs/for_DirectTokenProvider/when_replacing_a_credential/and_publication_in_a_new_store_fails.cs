// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_replacing_a_credential;

public class and_publication_in_a_new_store_fails : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    readonly IDirectSecretStore _newStore = Substitute.For<IDirectSecretStore>();
    Exception _error = null!;

    void Establish() => _server.Store(_target, "previous-refresh");

    async Task Because() => _error = await Catch.Exception(() => _server.Provider(store: _newStore).Replace(_target, new DirectTokens("new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), _server.Provider(), CancellationToken.None, () => throw new IOException("config cannot be replaced")));

    [Fact] void should_report_the_failure() => _error.ShouldBeOfExactType<IOException>();
    [Fact] void should_remove_the_new_store_entry() => _newStore.Received(1).Delete(_target.Key, CancellationToken.None);
    [Fact] void should_retain_the_previous_secret() => JsonSerializer.Deserialize<DirectTokens>(_server.Stored[_target.Key])!.RefreshToken.ShouldEqual("previous-refresh");
    [Fact] void should_revoke_only_the_new_token() => _server.Revoked.ShouldContainOnly(["new-refresh"]);

    void Destroy() => _server.Dispose();
}
