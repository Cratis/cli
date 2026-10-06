// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_replacing_a_credential;

public class and_a_canceled_write_already_committed : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    Exception _error = null!;
    string? _value;
    bool _published;

    async Task Because()
    {
        _value = JsonSerializer.Serialize(new DirectTokens("previous-access", "previous-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read", "https://identity.example/"));
        var store = Substitute.For<IDirectSecretStore>();
        store.Read(_target.Key, Arg.Any<CancellationToken>()).Returns(_ => _value);
        var writes = 0;
        store.Write(_target.Key, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _value = call.ArgAt<string>(1);
            return ++writes == 1 ? Task.FromException(new OperationCanceledException()) : Task.CompletedTask;
        });
        var provider = _server.Provider(store: store);
        _error = await Catch.Exception(() => provider.Replace(_target, new DirectTokens("new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), provider, CancellationToken.None, () => _published = true));
    }

    [Fact] void should_restore_the_previous_credential() => JsonSerializer.Deserialize<DirectTokens>(_value!)!.RefreshToken.ShouldEqual("previous-refresh");
    [Fact] void should_revoke_the_new_token() => _server.Revoked.ShouldContainOnly(["new-refresh"]);
    [Fact] void should_not_publish_the_failed_login() => _published.ShouldBeFalse();
    [Fact] void should_preserve_the_cancellation_outcome() => _error.ShouldBeOfExactType<OperationCanceledException>();

    void Destroy() => _server.Dispose();
}
