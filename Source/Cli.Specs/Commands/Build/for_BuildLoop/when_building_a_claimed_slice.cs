// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildLoop;

public class when_building_a_claimed_slice : given.a_builder_client
{
    BuildOrder? _built;
    BuildGitInstructions? _git;
    int _count;
    List<string> _log;

    void Establish()
    {
        _log = [];
        _studio.Answer("connect", HttpStatusCode.Accepted, """{ "reason": "Waiting for approval.", "code": "K7PM-3QXR" }""");
        _studio.Answer("connect", HttpStatusCode.OK, """{ "name": "Build box", "protocolVersion": 1, "leaseSeconds": 90 }""");
        _studio.Answer("work", HttpStatusCode.OK, Work);
        _studio.Answer($"work/{given.an_order.SliceId}/outcome", HttpStatusCode.OK, """{ "settled": "built" }""");
    }

    async Task Because()
    {
        var loop = new BuildLoop(
            _client,
            (order, git, _) =>
            {
                (_built, _git) = (order, git);
                return Task.FromResult(new JsonObject { ["contractVersion"] = "1", ["orderId"] = order.OrderId, ["kind"] = "built", ["files"] = new JsonArray(), ["specifications"] = new JsonArray(), ["assumptions"] = new JsonArray(), ["commit"] = "abc" });
            },
            _log.Add,
            (_, cancellationToken) => Task.Delay(1, cancellationToken));
        _count = await loop.Run(BuilderManifests.For(null, "generator"), once: true, CancellationToken.None);
    }

    [Fact] void should_wait_for_approval_and_show_the_code() => _log.ShouldContain(line => line.Contains("K7PM-3QXR", StringComparison.Ordinal));
    [Fact] void should_build_the_claimed_order() => _built!.OrderId.ShouldEqual(given.an_order.OrderId);
    [Fact] void should_hand_over_the_git_instructions() => _git!.IsStudioManaged.ShouldBeTrue();
    [Fact] void should_report_the_outcome() => JsonNode.Parse(_studio.BodiesTo($"work/{given.an_order.SliceId}/outcome").Single())!["commit"]!.GetValue<string>().ShouldEqual("abc");
    [Fact] void should_count_the_slice_built() => _count.ShouldEqual(1);
    [Fact] void should_say_goodbye() => _studio.Requests.Last().Path.ShouldEqual("disconnect");
}
