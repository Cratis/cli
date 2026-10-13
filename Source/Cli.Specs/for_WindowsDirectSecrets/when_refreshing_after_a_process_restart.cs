// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_WindowsDirectSecrets.given;

namespace Cratis.Cli.for_WindowsDirectSecrets;

public class when_refreshing_after_a_process_restart : Specification
{
    readonly a_fake_credential_api _api = new();
    string _access = null!;
    string _secondAccess = null!;
    string _sentRefresh = null!;
    string _rotated = null!;
    int _exchanges;

    async Task Because()
    {
        var target = DirectTarget.Create("https://direct.example", "team");
        var issuer = new Uri("https://identity.example/");
        using var http = new HttpClient(new Handler(async request =>
        {
            if (request.RequestUri!.AbsolutePath == "/.well-known/oauth-authorization-server")
            {
                return Json("{\"issuer\":\"https://identity.example/\",\"authorization_endpoint\":\"https://identity.example/authorize\",\"token_endpoint\":\"https://identity.example/token\",\"revocation_endpoint\":\"https://identity.example/revoke\"}");
            }

            _exchanges++;
            _sentRefresh = await request.Content!.ReadAsStringAsync();
            return Json($"{{\"access_token\":\"{new string('b', 6000)}\",\"refresh_token\":\"rotated-refresh\",\"token_type\":\"Bearer\",\"expires_in\":3600}}");
        }));
        var store = new WindowsDirectSecrets(_api);
        var initial = new DirectTokenProvider(store, new Lock(), new DirectDiscovery(http), http, issuer);
        await initial.Save(target, new DirectTokens(new string('a', 6000), "original-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), CancellationToken.None);
        var restarted = new DirectTokenProvider(new WindowsDirectSecrets(_api), new Lock(), new DirectDiscovery(http), http, issuer);
        _access = await restarted.GetAccessToken(target, issuer, CancellationToken.None);
        _secondAccess = await restarted.GetAccessToken(target, issuer, CancellationToken.None);
        var nextProcess = new DirectTokenProvider(new WindowsDirectSecrets(_api), new Lock(), new DirectDiscovery(http), http, issuer);
        _rotated = (await nextProcess.Read(target, CancellationToken.None))!.RefreshToken;
    }

    [Fact] void should_use_the_original_refresh_token() => _sentRefresh.ShouldContain("refresh_token=original-refresh");
    [Fact] void should_return_the_new_access_token() => _access.ShouldEqual(new string('b', 6000));
    [Fact] void should_reuse_the_new_access_token_in_memory() => _secondAccess.ShouldEqual(new string('b', 6000));
    [Fact] void should_persist_the_rotated_refresh_token() => _rotated.ShouldEqual("rotated-refresh");
    [Fact] void should_only_refresh_once_in_the_process() => _exchanges.ShouldEqual(1);
    [Fact] void should_keep_each_credential_below_the_limit() => (_api.LargestBlob <= 2560).ShouldBeTrue();

    static HttpResponseMessage Json(string content) => new(HttpStatusCode.OK) { Content = new StringContent(content) };

    sealed class Lock : IDirectRefreshLock
    {
        public Task<IAsyncDisposable> Acquire(string key, CancellationToken cancellationToken) => Task.FromResult<IAsyncDisposable>(new Lease());
    }

    sealed class Lease : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> answer) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => answer(request);
    }
}
