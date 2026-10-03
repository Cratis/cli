// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectDiscovery.when_the_issuer_uses_plain_http;

public class on_a_remote_host : Specification
{
    int _requests;
    Exception _error = null!;

    async Task Because()
    {
        using var http = new HttpClient(new Handler(() => _requests++));
        _error = await Catch.Exception(() => new DirectDiscovery(http).DiscoverIssuer(new Uri("http://identity.example/"), CancellationToken.None));
    }

    [Fact] void should_refuse_the_issuer() => _error.ShouldBeOfExactType<DirectAuthError>();
    [Fact] void should_not_contact_the_issuer() => _requests.ShouldEqual(0);

    sealed class Handler(Action onRequest) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            onRequest();
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));
        }
    }
}
