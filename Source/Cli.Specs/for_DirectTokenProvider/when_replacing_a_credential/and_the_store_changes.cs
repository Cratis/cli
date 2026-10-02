// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_replacing_a_credential;

public class and_the_store_changes : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    readonly IDirectSecretStore _newStore = Substitute.For<IDirectSecretStore>();
    bool _previousRetainedAtPublication;

    void Establish() => _server.Store(_target, "previous-refresh");

    async Task Because() => await _server.Provider(store: _newStore).Replace(_target, new DirectTokens("new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), _server.Provider(), CancellationToken.None, () => _previousRetainedAtPublication = _server.Stored.ContainsKey(_target.Key));

    [Fact] void should_keep_the_previous_secret_until_publication() => _previousRetainedAtPublication.ShouldBeTrue();
    [Fact] void should_save_the_new_secret() => _newStore.Received(1).Write(_target.Key, Arg.Any<string>(), CancellationToken.None);
    [Fact] void should_not_delete_the_new_secret() => _newStore.DidNotReceive().Delete(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] void should_remove_the_previous_store_entry_after_publication() => _server.Stored.ShouldBeEmpty();
    [Fact] void should_revoke_only_the_previous_token() => _server.Revoked.ShouldContainOnly(["previous-refresh"]);

    void Destroy() => _server.Dispose();
}
