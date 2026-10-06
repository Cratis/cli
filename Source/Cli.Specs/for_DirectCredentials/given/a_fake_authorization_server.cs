// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectCredentials.given;

/// <summary>An in-memory authorization server and credential store; no network is used.</summary>
internal sealed class a_fake_authorization_server : IDisposable
{
    readonly HttpClient _http;

    public a_fake_authorization_server() => _http = new HttpClient(new Handler(this));

    public List<string> Revoked { get; } = [];
    public HashSet<string> RefusedTokens { get; } = [];
    public Dictionary<string, string> Stored { get; } = [];
    public HttpStatusCode RefusalStatus { get; set; } = HttpStatusCode.InternalServerError;
    public List<Uri> Requests { get; } = [];
    public Exception? ReadFailure { get; set; }
    public Exception? WriteFailure { get; set; }
    public Exception? DeleteFailure { get; set; }
    public Action<string>? BeforeRevocation { get; set; }
    public string? DiscoveryResponse { get; set; }

    public void Store(DirectTarget target, string refreshToken, string? issuer = "https://identity.example/", DateTimeOffset? expiresAt = null) =>
        Stored[target.Key] = JsonSerializer.Serialize(new DirectTokens("access", refreshToken, expiresAt ?? DateTimeOffset.UtcNow.AddHours(1), "direct:read", issuer));

    public DirectTokenProvider Provider(string issuer = "https://identity.example/", IDirectSecretStore? store = null) => new(store ?? new FakeStore(this), new Lock(), new DirectDiscovery(_http), _http, new Uri(issuer));

    public void Dispose() => _http.Dispose();

    sealed class Handler(a_fake_authorization_server server) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            server.Requests.Add(request.RequestUri!);
            if (request.RequestUri!.AbsolutePath == "/revoke")
            {
                var form = await request.Content!.ReadAsStringAsync(cancellationToken);
                var token = form.Split('&').Select(pair => pair.Split('=')).First(pair => pair[0] == "token")[1];
                server.BeforeRevocation?.Invoke(token);
                if (server.RefusedTokens.Contains(token))
                {
                    return new HttpResponseMessage(server.RefusalStatus);
                }

                server.Revoked.Add(token);
                return new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(server.DiscoveryResponse ?? "{\"issuer\":\"https://identity.example/\",\"authorization_endpoint\":\"https://identity.example/authorize\",\"token_endpoint\":\"https://identity.example/token\",\"revocation_endpoint\":\"https://identity.example/revoke\"}")
            };
        }
    }

    sealed class FakeStore(a_fake_authorization_server server) : IDirectSecretStore
    {
        public Task<string?> Read(string key, CancellationToken cancellationToken) =>
            server.ReadFailure is { } failure ? Task.FromException<string?>(failure) : Task.FromResult(server.Stored.GetValueOrDefault(key));

        public Task Write(string key, string value, CancellationToken cancellationToken)
        {
            if (server.WriteFailure is { } failure)
            {
                return Task.FromException(failure);
            }

            server.Stored[key] = value;
            return Task.CompletedTask;
        }

        public Task Delete(string key, CancellationToken cancellationToken)
        {
            if (server.DeleteFailure is { } failure)
            {
                return Task.FromException(failure);
            }

            server.Stored.Remove(key);
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
