// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectTokenProvider;

public class when_two_callers_refresh_the_same_expired_session : Specification
{
    int _exchanges;
    string[] _access = null!;
    string _refresh = null!;
    string _request = null!;

    async Task Because()
    {
        var target = DirectTarget.Create("https://direct.example", "team");
        var store = new Store();
        using var http = new HttpClient(new Handler(async request =>
        {
            if (request.RequestUri!.AbsolutePath == "/.well-known/oauth-authorization-server")
            {
                return Json("{\"issuer\":\"https://identity.example/\",\"authorization_endpoint\":\"https://identity.example/authorize\",\"token_endpoint\":\"https://identity.example/token\",\"revocation_endpoint\":\"https://identity.example/revoke\"}");
            }

            Interlocked.Increment(ref _exchanges);
            _request = await request.Content!.ReadAsStringAsync();
            return Json("{\"access_token\":\"new-access\",\"refresh_token\":\"rotated-refresh\",\"token_type\":\"Bearer\",\"scope\":\"direct:read\",\"expires_in\":3600}");
        }));
        var provider = new DirectTokenProvider(store, new Lock(), new DirectDiscovery(http), http, new Uri("https://identity.example/"));
        await provider.Save(target, new DirectTokens("old-access", "old-refresh", DateTimeOffset.UtcNow.AddMinutes(-1), "direct:read"), CancellationToken.None);
        _access = await Task.WhenAll(
            provider.GetAccessToken(target, new Uri("https://identity.example/"), CancellationToken.None),
            provider.GetAccessToken(target, new Uri("https://identity.example/"), CancellationToken.None));
        _refresh = (await provider.Read(target, CancellationToken.None))!.RefreshToken;
    }

    [Fact] void should_return_the_new_access_token_to_the_first_caller() => _access[0].ShouldEqual("new-access");
    [Fact] void should_return_the_new_access_token_to_the_second_caller() => _access[1].ShouldEqual("new-access");
    [Fact] void should_only_exchange_the_original_refresh_token_once() => _exchanges.ShouldEqual(1);
    [Fact] void should_save_the_rotated_refresh_token() => _refresh.ShouldEqual("rotated-refresh");
    [Fact] void should_bind_the_refresh_to_the_direct_resource() => _request.ShouldContain("resource=https%3A%2F%2Fdirect.example%2Fmcp");

    static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value) };

    sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => answer(request);
    }

    sealed class Store : IDirectSecretStore
    {
        string? _value;
        public Task<string?> Read(string key, CancellationToken cancellationToken) => Task.FromResult(_value);
        public Task Write(string key, string value, CancellationToken cancellationToken)
        {
            _value = value;
            return Task.CompletedTask;
        }
        public Task Delete(string key, CancellationToken cancellationToken)
        {
            _value = null;
            return Task.CompletedTask;
        }
    }

    sealed class Lock : IDirectRefreshLock
    {
        readonly SemaphoreSlim _semaphore = new(1, 1);
        public async Task<IAsyncDisposable> Acquire(string key, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            return new Lease(_semaphore);
        }
    }

    sealed class Lease(SemaphoreSlim semaphore) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            semaphore.Release();
            return ValueTask.CompletedTask;
        }
    }
}
