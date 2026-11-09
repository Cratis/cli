// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildLoop;

public class when_studio_refuses_the_outcome : given.a_builder_client
{
    void Establish()
    {
        _studio.Answer("connect", HttpStatusCode.OK, """{ "name": "Build box", "protocolVersion": 1, "leaseSeconds": 90 }""");
        _studio.Answer("work", HttpStatusCode.OK, Work);
        _studio.Answer($"work/{given.an_order.SliceId}/outcome", HttpStatusCode.BadRequest, """{ "reason": "The outcome cannot be read.", "problems": ["/files: required"] }""");
        _studio.Answer($"work/{given.an_order.SliceId}/outcome", HttpStatusCode.OK, """{ "settled": "failed" }""");
    }

    async Task Because()
    {
        var loop = new BuildLoop(_client, (order, _, _) => Task.FromResult(new JsonObject { ["kind"] = "built" }), _ => { }, (_, cancellationToken) => Task.Delay(1, cancellationToken));
        await loop.Run(BuilderManifests.For(null, "generator"), once: true, CancellationToken.None);
    }

    [Fact] void should_report_a_retryable_failure_instead() => JsonNode.Parse(_studio.BodiesTo($"work/{given.an_order.SliceId}/outcome").Last())!["kind"]!.GetValue<string>().ShouldEqual(BuildOutcomes.Failed);
    [Fact] void should_say_what_studio_refused() => _studio.BodiesTo($"work/{given.an_order.SliceId}/outcome").Last().ShouldContain("/files: required");
}
