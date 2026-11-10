// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildLoop;

public class when_the_lease_is_lost : given.a_builder_client
{
    bool _stopped;
    int _count;

    void Establish()
    {
        _studio.Answer("connect", HttpStatusCode.OK, """{ "name": "Build box", "protocolVersion": 1, "leaseSeconds": 30 }""");
        _studio.Answer("work", HttpStatusCode.OK, Work);
        _studio.Answer($"work/{given.an_order.SliceId}/lease", HttpStatusCode.Gone, """{ "reason": "The lease ran out." }""");
    }

    async Task Because()
    {
        var loop = new BuildLoop(
            _client,
            async (_, _, cancellationToken) =>
            {
                try
                {
                    await Task.Delay(Timeout.Infinite, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _stopped = true;
                    throw;
                }

                return [];
            },
            _ => { },
            (_, cancellationToken) => Task.Delay(1, cancellationToken));
        _count = await loop.Run(BuilderManifests.For(null, "generator"), once: true, CancellationToken.None);
    }

    [Fact] void should_stop_the_build() => _stopped.ShouldBeTrue();
    [Fact] void should_report_nothing() => _studio.BodiesTo($"work/{given.an_order.SliceId}/outcome").ShouldBeEmpty();
    [Fact] void should_count_nothing_built() => _count.ShouldEqual(0);
}
