// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_StudioBuilderClient;

public class when_reporting_progress : given.a_builder_client
{
    const string Assignment = "assignment-not-first-scope-slice";
    readonly string _path = $"work/{Assignment}/progress";
    bool _accepted;
    bool _lost;
    Exception? _refused;
    Exception? _invalid;

    async Task Because()
    {
        var document = JsonNode.Parse(given.an_order.Json())!.AsObject();
        document["contractVersion"] = "2";
        var order = BuildOrder.From(document);
        var work = new StudioBuilderWork(Assignment, DateTimeOffset.UtcNow.AddMinutes(1), BuildGitInstructions.Offline, order);
        _accepted = await _client.Progress(work, new(new string('x', 1990) + Token, [new(Token)], new(3, 1)), BuildRedaction.Patterns, CancellationToken.None);
        _studio.Answer(_path, HttpStatusCode.Gone);
        _lost = await _client.Progress(work, new("Building"), BuildRedaction.Patterns, CancellationToken.None);
        _studio.Answer(_path, HttpStatusCode.BadRequest, "Malformed report");
        _refused = await Catch.Exception(async () => await _client.Progress(work, new("Building"), BuildRedaction.Patterns, CancellationToken.None));
        _invalid = await Catch.Exception(async () => await _client.Progress(work, new(Usage: new(-1, 0)), BuildRedaction.Patterns, CancellationToken.None));
        await _client.Progress(work with { Order = given.an_order.Parse() }, new("Building"), BuildRedaction.Patterns, CancellationToken.None);
        await _client.Progress(work, new(), BuildRedaction.Patterns, CancellationToken.None);
    }

    [Fact] void should_accept_success() => _accepted.ShouldBeTrue();
    [Fact] void should_recognize_a_lost_lease() => _lost.ShouldBeFalse();
    [Fact] void should_distinguish_schema_refusal() => _refused.ShouldBeOfExactType<StudioBuilderRefused>();
    [Fact] void should_reject_invalid_usage_locally() => _invalid.ShouldBeOfExactType<ArgumentException>();
    [Fact] void should_send_neither_contract_one_nor_empty_nor_malformed_progress() => _studio.BodiesTo(_path).Count().ShouldEqual(3);
    [Fact] void should_redact_the_builder_credential_before_clipping() => _studio.BodiesTo(_path).First().Contains(Token[..10], StringComparison.Ordinal).ShouldBeFalse();
    [Fact] void should_authenticate_every_request() => _studio.Credentials.All(request => request.Credential == $"{BuilderId}.{Token}").ShouldBeTrue();
    [Fact] void should_use_the_assignment_endpoint() => _studio.Requests.All(request => request.Path == _path).ShouldBeTrue();
    [Fact] void should_stamp_the_held_order() => JsonNode.Parse(_studio.BodiesTo(_path).First())!["orderId"]!.GetValue<string>().ShouldEqual(given.an_order.OrderId);
}
