// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildLoop;

public class when_reporting_live_progress : given.a_builder_client
{
    readonly TaskCompletionSource _received = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly List<string> _log = [];
    int _count;
    bool _beforeCompletion;
    Action<BuildProgress> _oldCallback;
    string ProgressPath => $"work/{given.an_order.SliceId}/progress";

    void Establish()
    {
        _studio.Answer("connect", HttpStatusCode.OK, """{ "name": "Builder", "protocolVersion": 1, "leaseSeconds": 90 }""");
        var work = JsonNode.Parse(Work)!;
        work["order"]!["contractVersion"] = "2";
        _studio.Answer("work", HttpStatusCode.OK, work.ToJsonString());
        _studio.Answer(ProgressPath, HttpStatusCode.BadRequest, "Rejected");
        _studio.Answer(ProgressPath, HttpStatusCode.ServiceUnavailable, "Unavailable");
        _studio.OnRequest = path => { if (path == ProgressPath && _studio.BodiesTo(ProgressPath).Count() == 3) _received.TrySetResult(); };
    }

    async Task Because()
    {
        var loop = new BuildLoop(
            _client,
            (_, _, _) => throw new InvalidOperationException(),
            _log.Add,
            buildWithProgress: async (order, _, progress, token) =>
            {
                _oldCallback = progress;
                progress(new("Preparing"));
                progress(new("Building"));
                progress(new("Verifying"));
                await _received.Task.WaitAsync(TimeSpan.FromSeconds(5), token);
                _beforeCompletion = true;
                return BuildOutcomes.Failure(order, "Fixture failure", false);
            });
        _count = await loop.Run(BuilderManifests.For(null, "generator"), true, CancellationToken.None);
        _oldCallback(new("Late step"));
    }

    [Fact] void should_report_before_the_agent_finishes() => _beforeCompletion.ShouldBeTrue();
    [Fact] void should_not_count_a_failed_build_as_successful() => _count.ShouldEqual(0);
    [Fact] void should_work_with_connection_protocol_one() => _studio.BodiesTo($"work/{given.an_order.SliceId}/outcome").Count().ShouldEqual(1);
    [Fact] void should_keep_building_after_refusals() => _log.Count(line => line.Contains("not delivered", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_not_send_late_callbacks() => _studio.BodiesTo(ProgressPath).Count().ShouldEqual(3);
    [Fact] void should_drain_progress_before_outcome() => _studio.Requests.Select(request => request.Path).ToArray().ShouldEqual(["connect", "work", ProgressPath, ProgressPath, ProgressPath, $"work/{given.an_order.SliceId}/outcome", "disconnect"]);
}
