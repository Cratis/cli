// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;
using Cratis.Cli.for_DirectCredentials.given;

namespace Cratis.Cli.for_DirectTokenProvider.when_replacing_a_credential;

public class and_acquiring_the_lock_and_recovery_fail : Specification
{
    readonly a_fake_authorization_server _server = new();
    readonly DirectTarget _target = DirectTarget.Create("https://direct.example", "team");
    Exception _error = null!;

    async Task Because()
    {
        var refreshLock = Substitute.For<IDirectRefreshLock>();
        refreshLock.Acquire(_target.Key, Arg.Any<CancellationToken>()).Returns(Task.FromException<IAsyncDisposable>(new IOException("lock unavailable")));
        using var http = new HttpClient(new Handler());
        var provider = new DirectTokenProvider(Substitute.For<IDirectSecretStore>(), refreshLock, new DirectDiscovery(http), http, new Uri("https://identity.example/"));
        _error = await Catch.Exception(() => provider.Replace(_target, new DirectTokens("new-access", "new-refresh", DateTimeOffset.UtcNow.AddHours(1), "direct:read"), _server.Provider(), CancellationToken.None));
    }

    [Fact] void should_report_incomplete_recovery() => _error.Message.ShouldContain("The new Direct refresh token could not be revoked");
    [Fact] void should_report_a_safe_authentication_error() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_not_disclose_the_new_token() => _error.Message.ShouldNotContain("new-refresh");

    void Destroy() => _server.Dispose();

    sealed class Handler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
    }
}
