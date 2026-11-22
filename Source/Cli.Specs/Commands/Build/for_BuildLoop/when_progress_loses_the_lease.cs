// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildLoop;

public class when_progress_loses_the_lease : given.a_builder_client
{
    int _count;
    bool _cancelled;

    void Establish()
    {
        _studio.Answer("connect", HttpStatusCode.OK, """{ "name": "Builder", "protocolVersion": 1, "leaseSeconds": 90 }""");
        var work = JsonNode.Parse(Work)!;
        work["order"]!["contractVersion"] = "2";
        _studio.Answer("work", HttpStatusCode.OK, work.ToJsonString());
        _studio.Answer($"work/{given.an_order.SliceId}/progress", HttpStatusCode.Gone);
    }

    async Task Because()
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var loop = new BuildLoop(
            _client,
            (_, _, _) => throw new InvalidOperationException(),
            _ => { },
            buildWithProgress: async (order, _, progress, token) =>
            {
                progress(new("Building"));
                try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
                catch (OperationCanceledException)
                {
                    _cancelled = true;
                    throw;
                }
                return BuildOutcomes.Failure(order, "Unreachable", false);
            });
        _count = await loop.Run(BuilderManifests.For(null, "generator"), true, deadline.Token);
    }

    [Fact] void should_cancel_the_held_build() => _cancelled.ShouldBeTrue();
    [Fact] void should_not_report_an_outcome() => _studio.BodiesTo($"work/{given.an_order.SliceId}/outcome").ShouldBeEmpty();
    [Fact] void should_not_count_the_lost_assignment() => _count.ShouldEqual(0);
    [Fact] void should_join_and_disconnect() => _studio.Requests.Last().Path.ShouldEqual("disconnect");
}
