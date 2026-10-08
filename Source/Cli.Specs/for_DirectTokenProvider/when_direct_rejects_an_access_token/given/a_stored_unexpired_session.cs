// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectTokenProvider.when_direct_rejects_an_access_token.given;

public class a_stored_unexpired_session : Specification
{
    private protected static readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    private protected static readonly Uri _issuer = new("https://identity.example/");
    private protected DirectTokenProvider _provider;
    protected int _exchanges;
    protected string _refresh;
    HttpClient _http;

    async Task Establish()
    {
        _http = new HttpClient(new Handler(request =>
        {
            if (request.RequestUri!.AbsolutePath == "/.well-known/oauth-authorization-server")
            {
                return Json("{\"issuer\":\"https://identity.example/\",\"authorization_endpoint\":\"https://identity.example/authorize\",\"token_endpoint\":\"https://identity.example/token\",\"revocation_endpoint\":\"https://identity.example/revoke\"}");
            }

            Interlocked.Increment(ref _exchanges);
            return Json("{\"access_token\":\"new-access\",\"refresh_token\":\"rotated-refresh\",\"token_type\":\"Bearer\",\"expires_in\":3600}");
        }));
        _provider = new DirectTokenProvider(new Store(), new Lock(), new DirectDiscovery(_http), _http, _issuer);
        await _provider.Save(_target, new DirectTokens("current-access", "current-refresh", DateTimeOffset.UtcNow.AddMinutes(30), "direct:read"), CancellationToken.None);
    }

    protected async Task<string> StoredRefreshToken() => (await _provider.Read(_target, CancellationToken.None))!.RefreshToken;

    void Destroy() => _http.Dispose();

    static HttpResponseMessage Json(string value) => new(HttpStatusCode.OK) { Content = new StringContent(value) };

    sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(answer(request));
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
        public Task<IAsyncDisposable> Acquire(string key, CancellationToken cancellationToken) => Task.FromResult<IAsyncDisposable>(new Lease());
    }

    sealed class Lease : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
