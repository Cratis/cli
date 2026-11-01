// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.for_StageReadiness;

public class when_reading_runtime_status : Specification
{
    [Theory]
    [InlineData("warm", true, false)]
    [InlineData("loading", true, false)]
    [InlineData("ready", false, false)]
    [InlineData("unsupported", false, true)]
    async Task should_read_each_admission_state(string state, bool pending, bool unsupported)
    {
        using var client = new HttpClient(new Response(HttpStatusCode.OK, $"{{\"state\":\"{state}\",\"engine\":\"semantic\"}}"));
        var result = await StageReadiness.ReadStatus(client, 9090, CancellationToken.None);
        result!.State.ShouldEqual(state);
        result.Engine.ShouldEqual("semantic");
        result.IsPending.ShouldEqual(pending);
        result.IsUnsupported.ShouldEqual(unsupported);
    }

    [Theory]
    [InlineData(404, "not found")]
    [InlineData(200, "<html>older image</html>")]
    [InlineData(200, "{}")]
    [InlineData(200, "null")]
    [InlineData(200, "{\"state\":\"unknown\"}")]
    async Task should_fall_back_for_images_without_the_status_contract(int code, string body)
    {
        using var client = new HttpClient(new Response((HttpStatusCode)code, body));
        (await StageReadiness.ReadStatus(client, 9090, CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    async Task should_keep_waiting_after_a_server_error()
    {
        using var client = new HttpClient(new Response(HttpStatusCode.InternalServerError, "unavailable"));
        (await StageReadiness.ReadStatus(client, 9090, CancellationToken.None))!.IsPending.ShouldBeTrue();
    }

    [Fact]
    async Task should_preserve_caller_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var client = new HttpClient(new Response(HttpStatusCode.OK, "{}"));
        var error = await Catch.Exception(() => StageReadiness.ReadStatus(client, 9090, cancellation.Token));
        (error is OperationCanceledException).ShouldBeTrue();
    }

    sealed class Response(HttpStatusCode code, string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            request.RequestUri!.AbsolutePath.ShouldEqual("/stage/status");
            return Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(body) });
        }
    }
}
