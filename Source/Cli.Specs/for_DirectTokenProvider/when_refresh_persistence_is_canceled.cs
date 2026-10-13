// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectTokenProvider;

public class when_refresh_persistence_is_canceled : Specification
{
    string _access = null!;
    string _refresh = null!;
    bool _heldDuringPersistence;
    bool _callerCanceled;

    async Task Because()
    {
        using var caller = new CancellationTokenSource();
        var target = DirectTarget.Create("https://direct.example", "team");
        var store = Substitute.For<IDirectSecretStore>();
        var old = JsonSerializer.Serialize(new DirectTokens("old-access", "old-refresh", DateTimeOffset.UtcNow.AddMinutes(-1), "direct:read", "https://identity.example/"));
        store.Read(target.Key, Arg.Any<CancellationToken>()).Returns(old);
        var released = false;
        var lease = new Lease(() => released = true);
        var refreshLock = Substitute.For<IDirectRefreshLock>();
        refreshLock.Acquire(target.Key, Arg.Any<CancellationToken>()).Returns(lease);
        store.Write(target.Key, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(async call =>
        {
            // Cancel after the successful exchange, at the persistence boundary.
            await caller.CancelAsync();
            call.ArgAt<CancellationToken>(2).ThrowIfCancellationRequested();
            _heldDuringPersistence = !released;
            _refresh = JsonSerializer.Deserialize<DirectTokens>(call.ArgAt<string>(1))!.RefreshToken;
        });
        using var http = new HttpClient(new Handler());
        var issuer = new Uri("https://identity.example/");
        var provider = new DirectTokenProvider(store, refreshLock, new DirectDiscovery(http), http, issuer);
        _access = await provider.GetAccessToken(target, issuer, caller.Token);
        _callerCanceled = caller.IsCancellationRequested;
    }

    [Fact] void should_finish_even_after_caller_cancellation() => _access.ShouldEqual("new-access");
    [Fact] void should_persist_the_rotated_refresh_token() => _refresh.ShouldEqual("rotated-refresh");
    [Fact] void should_persist_before_releasing_the_lock() => _heldDuringPersistence.ShouldBeTrue();
    [Fact] void should_exercise_caller_cancellation() => _callerCanceled.ShouldBeTrue();

    sealed class Lease(Action release) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            release();
            return ValueTask.CompletedTask;
        }
    }

    sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(request.RequestUri!.AbsolutePath == "/token"
                ? "{\"access_token\":\"new-access\",\"refresh_token\":\"rotated-refresh\",\"token_type\":\"Bearer\",\"expires_in\":3600}"
                : "{\"issuer\":\"https://identity.example/\",\"authorization_endpoint\":\"https://identity.example/authorize\",\"token_endpoint\":\"https://identity.example/token\",\"revocation_endpoint\":\"https://identity.example/revoke\"}")
        });
    }
}
