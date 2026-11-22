// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildLoop;

public class when_cancelling_with_progress_in_flight : Specification
{
    readonly given.a_fake_studio _studio = new();
    readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _heartbeat = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _requestCancelled;
    Exception? _error;

    async Task Because()
    {
        _studio.Answer("connect", HttpStatusCode.OK, """{ "name": "Builder", "protocolVersion": 1, "leaseSeconds": 90 }""");
        var work = JsonNode.Parse(given.a_builder_client.Work)!;
        work["order"]!["contractVersion"] = "2";
        _studio.Answer("work", HttpStatusCode.OK, work.ToJsonString());
        _studio.OnRequest = path => { if (path.EndsWith("/lease", StringComparison.Ordinal)) _heartbeat.TrySetResult(); };
        using var handler = new held_progress(_studio, _started, () => _requestCancelled = true);
        using var http = new HttpClient(handler);
        var client = new StudioBuilderClient(http, new Uri("https://studio.example"), "acme");
        client.Authenticate(given.a_builder_client.BuilderId, given.a_builder_client.Token);
        using var caller = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var waits = 0;
        var loop = new BuildLoop(
            client,
            (_, _, _) => throw new InvalidOperationException(),
            _ => { },
            delay: (_, token) => Interlocked.Increment(ref waits) == 1 ? _started.Task.WaitAsync(token) : Task.Delay(Timeout.InfiniteTimeSpan, token),
            buildWithProgress: async (order, _, progress, token) =>
            {
                progress(new("Building"));
                await _heartbeat.Task.WaitAsync(token);
                await caller.CancelAsync();
                token.ThrowIfCancellationRequested();
                return BuildOutcomes.Failure(order, "Unreachable", false);
            });
        _error = await Catch.Exception(async () => await loop.Run(BuilderManifests.For(null, "generator"), true, caller.Token));
    }

    [Fact] void should_heartbeat_without_waiting_for_progress() => _heartbeat.Task.IsCompletedSuccessfully.ShouldBeTrue();
    [Fact] void should_cancel_the_in_flight_request() => _requestCancelled.ShouldBeTrue();
    [Fact] void should_finish_caller_cancellation_gracefully() => _error.ShouldBeNull();
    [Fact] void should_not_report_an_outcome_after_cancellation() => _studio.BodiesTo($"work/{given.an_order.SliceId}/outcome").ShouldBeEmpty();

    sealed class held_progress(HttpMessageHandler inner, TaskCompletionSource started, Action cancelled) : DelegatingHandler(inner)
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.RequestUri!.AbsolutePath.EndsWith("/progress", StringComparison.Ordinal)) return await base.SendAsync(request, cancellationToken);
            started.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken); }
            catch (OperationCanceledException)
            {
                cancelled();
                throw;
            }
            return new(HttpStatusCode.OK);
        }
    }
}
