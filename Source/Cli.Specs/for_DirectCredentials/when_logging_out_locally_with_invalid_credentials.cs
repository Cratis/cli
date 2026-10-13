// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials;

public class when_logging_out_locally_with_invalid_credentials : given.stored_credentials
{
    IReadOnlyList<DirectLogoutOutcome> _outcomes = null!;
    IDirectSecretStore _store = null!;
    readonly List<string> _deleted = [];
    bool _held;
    bool _lockedDeletion;

    async Task Because()
    {
        var entry = _config.Credentials[0];
        entry.Issuer = "not-an-issuer";
        _store = Substitute.For<IDirectSecretStore>();
        _store.Delete(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _lockedDeletion = _held;
            _deleted.Add(call.ArgAt<string>(0));
            return Task.CompletedTask;
        });
        var refreshLock = Substitute.For<IDirectRefreshLock>();
        var lease = new Lease(() => _held = false);
        refreshLock.Acquire(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            _held = true;
            return lease;
        });
        _outcomes = await DirectCredentials.Logout(
            _config,
            [entry],
            _ => throw new DirectAuthError("Revocation is unavailable."),
            CancellationToken.None,
            (credential, token) => DirectLoginFlow.ForgetLocally(credential, token, _ => _store, refreshLock));
    }

    [Fact] void should_delete_the_secret_without_reading_it() => _deleted.ShouldContainOnly([DirectTarget.Create("https://direct.example", "active").Key]);
    [Fact] void should_delete_under_the_refresh_lock() => _lockedDeletion.ShouldBeTrue();
    [Fact] void should_forget_the_index_entry() => _config.Credentials.Count.ShouldEqual(3);
    [Fact] void should_not_claim_server_side_revocation() => _outcomes[0].Revoked.ShouldBeFalse();
    [Fact] void should_allow_recovery_with_an_invalid_issuer() => _outcomes[0].Failure.ShouldBeNull();
    [Fact] void should_never_read_the_unreadable_secret() => _store.DidNotReceive().Read(Arg.Any<string>(), Arg.Any<CancellationToken>());

    sealed class Lease(Action release) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            release();
            return ValueTask.CompletedTask;
        }
    }
}
