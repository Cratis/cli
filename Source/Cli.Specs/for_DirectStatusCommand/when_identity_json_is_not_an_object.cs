// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectStatusCommand;

public class when_identity_json_is_not_an_object
{
    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("true")]
    [InlineData("42")]
    [InlineData("\"user\"")]
    public async Task should_reject_the_response_as_a_safe_authentication_error(string json)
    {
        using var http = new HttpClient(new Handler(json));
        var error = await Catch.Exception(() => DirectStatusCommand.GetIdentity(http, DirectTarget.Create("https://direct.example", "team"), "access", CancellationToken.None));
        error.ShouldBeOfExactType<DirectAuthError>();
        error.Message.ShouldEqual("Direct identity response must be a JSON object.");
        DirectLoginFlow.IsSafeFailure(error).ShouldBeTrue();
    }

    sealed class Handler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        });
    }
}
