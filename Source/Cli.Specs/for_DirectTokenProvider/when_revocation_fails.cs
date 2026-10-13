// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectTokenProvider;

public class when_revocation_fails : Specification
{
    Exception _error = null!;
    IDirectSecretStore _store = null!;

    async Task Because()
    {
        _store = Substitute.For<IDirectSecretStore>();
        var target = DirectTarget.Create("https://direct.example", "team");
        _store.Read(target.Key, Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(new DirectTokens("access", "refresh", DateTimeOffset.UtcNow.AddMinutes(2), "direct:read", "https://identity.example/")));
        using var http = new HttpClient(new Handler(request => new HttpResponseMessage(request.RequestUri!.AbsolutePath == "/revoke" ? HttpStatusCode.InternalServerError : HttpStatusCode.OK)
        {
            Content = new StringContent("{\"issuer\":\"https://identity.example/\",\"authorization_endpoint\":\"https://identity.example/authorize\",\"token_endpoint\":\"https://identity.example/token\",\"revocation_endpoint\":\"https://identity.example/revoke\"}")
        }));
        var locker = Substitute.For<IDirectRefreshLock>();
        locker.Acquire(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(new Lease());
        var provider = new DirectTokenProvider(_store, locker, new DirectDiscovery(http), http, new Uri("https://identity.example/"));
        _error = await Catch.Exception(() => provider.Revoke(target, CancellationToken.None));
    }

    [Fact] void should_fail_the_logout() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] async Task should_retain_local_credentials() => await _store.DidNotReceive().Delete(Arg.Any<string>(), Arg.Any<CancellationToken>());

    sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(answer(request));
    }

    sealed class Lease : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
