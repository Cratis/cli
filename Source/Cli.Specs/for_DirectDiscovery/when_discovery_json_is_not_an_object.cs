// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectDiscovery;

public class when_discovery_json_is_not_an_object
{
    [Theory]
    [InlineData("[]", false)]
    [InlineData("null", false)]
    [InlineData("[]", true)]
    [InlineData("null", true)]
    public async Task should_reject_resource_and_issuer_metadata_as_an_authentication_error(string json, bool issuerOnly)
    {
        using var http = new HttpClient(new Handler(json));
        var discovery = new DirectDiscovery(http);
        var error = await Catch.Exception(() => issuerOnly
            ? discovery.DiscoverIssuer(new Uri("https://identity.example/"), CancellationToken.None)
            : discovery.Discover(DirectTarget.Create("https://direct.example", null), null, CancellationToken.None));
        error.ShouldBeOfExactType<DirectAuthError>();
        error.Message.ShouldEqual("Discovery metadata must be a JSON object.");
    }

    sealed class Handler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        });
    }
}
