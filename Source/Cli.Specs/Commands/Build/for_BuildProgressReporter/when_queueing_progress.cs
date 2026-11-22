// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Cli.Commands.Build.for_BuildProgressReporter;

public class when_queueing_progress : Specification
{
    readonly List<string> _log = [];
    int _sent;

    async Task Because()
    {
        using var handler = new held_studio();
        using var http = new HttpClient(handler);
        var client = new StudioBuilderClient(http, new Uri("https://studio.example"), "acme");
        client.Authenticate(given.a_builder_client.BuilderId, given.a_builder_client.Token);
        var work = new StudioBuilderWork(given.an_order.SliceId, DateTimeOffset.UtcNow.AddMinutes(1), BuildGitInstructions.Offline, given.an_order.Parse());
        work.Order.Document["contractVersion"] = "2";
        using var lease = new CancellationTokenSource();
        await using var reporter = new BuildProgressReporter(client, work, lease, BuildRedaction.Patterns, _log.Add);
        reporter.Publish(new("First"));
        await handler.Received.Task.WaitAsync(TimeSpan.FromSeconds(5));
        reporter.Publish(new("First"));
        for (var index = 0; index < 100; index++) reporter.Publish(new($"Step {index}"));
        handler.Release.SetResult();
        await reporter.Complete();
        reporter.Publish(new("Too late"));
        _sent = handler.Count;
    }

    [Fact] void should_coalesce_repeated_steps_and_bound_the_queue() => _sent.ShouldEqual(65);
    [Fact] void should_diagnose_dropped_updates() => _log.Count.ShouldEqual(36);

    sealed class held_studio : HttpMessageHandler
    {
        public TaskCompletionSource Received { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Count { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Count++;
            Received.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return new(HttpStatusCode.OK) { Content = new StringContent("{}") };
        }
    }
}
