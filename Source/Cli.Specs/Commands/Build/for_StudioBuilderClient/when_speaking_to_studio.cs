// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_StudioBuilderClient;

public class when_speaking_to_studio : given.a_builder_client
{
    StudioBuilderRegistration _registration;
    StudioBuilderRefused _waiting;
    StudioBuilderWelcome _welcome;
    StudioBuilderWork? _nothing;
    StudioBuilderWork? _work;
    bool _held;
    bool _lost;
    string _settled;

    void Establish()
    {
        _studio.Answer("registrations", HttpStatusCode.Created, $$"""{ "builderId": "{{BuilderId}}", "code": "K7PM-3QXR", "expires": "2026-11-07T09:00:00+00:00" }""");
        _studio.Answer("connect", HttpStatusCode.Accepted, """{ "reason": "Waiting for approval.", "code": "K7PM-3QXR" }""");
        _studio.Answer("connect", HttpStatusCode.OK, """{ "builderId": "x", "name": "Build box", "protocolVersion": 1, "studioProtocolVersions": [1], "needsUpgrade": false, "waitSeconds": 20, "leaseSeconds": 90 }""");
        _studio.Answer("work", HttpStatusCode.NoContent);
        _studio.Answer("work", HttpStatusCode.OK, Work);
        _studio.Answer($"work/{given.an_order.SliceId}/lease", HttpStatusCode.OK, """{ "leaseExpires": "2026-11-06T09:04:00+00:00" }""");
        _studio.Answer($"work/{given.an_order.SliceId}/lease", HttpStatusCode.Gone, """{ "reason": "The lease ran out." }""");
        _studio.Answer($"work/{given.an_order.SliceId}/outcome", HttpStatusCode.OK, """{ "settled": "built" }""");
    }

    async Task Because()
    {
        _registration = await _client.Register("Build box", "box-1", Token, CancellationToken.None);
        _waiting = (StudioBuilderRefused)(await Catch.Exception(() => _client.Connect(BuilderManifests.For(null, "generator"), CancellationToken.None)))!;
        _welcome = await _client.Connect(BuilderManifests.For(null, "generator"), CancellationToken.None);
        _nothing = await _client.Work(CancellationToken.None);
        _work = await _client.Work(CancellationToken.None);
        _held = await _client.Renew(given.an_order.SliceId, given.an_order.OrderId, CancellationToken.None);
        _lost = await _client.Renew(given.an_order.SliceId, given.an_order.OrderId, CancellationToken.None);
        _settled = await _client.Report(given.an_order.SliceId, BuildOutcomes.Failure(given.an_order.Parse(), "x", true), CancellationToken.None);
    }

    [Fact] void should_send_the_token_it_created_when_registering() => JsonNode.Parse(_studio.BodiesTo("registrations").Single())!["token"]!.GetValue<string>().ShouldEqual(Token);
    [Fact] void should_register_without_a_credential() => _studio.Requests.First().Authorization.ShouldBeNull();
    [Fact] void should_read_the_approval_code() => _registration.Code.ShouldEqual("K7PM-3QXR");
    [Fact] void should_say_a_registration_awaits_approval() => _waiting.AwaitsApproval.ShouldBeTrue();
    [Fact] void should_carry_the_approval_code_while_waiting() => _waiting.Code.ShouldEqual("K7PM-3QXR");
    [Fact] void should_present_its_id_and_token() => _studio.Requests.Last().Authorization.ShouldEqual($"Bearer {BuilderId}.{Token}");
    [Fact] void should_offer_the_protocol_versions_it_speaks() => JsonNode.Parse(_studio.BodiesTo("connect").First())!["protocolVersions"]!.ToJsonString().ShouldEqual("[1]");
    [Fact] void should_read_the_lease() => _welcome.LeaseSeconds.ShouldEqual(90);
    [Fact] void should_read_no_work_as_nothing() => _nothing.ShouldBeNull();
    [Fact] void should_read_the_claimed_order() => _work!.Order.OrderId.ShouldEqual(given.an_order.OrderId);
    [Fact] void should_read_the_studio_managed_branch() => _work!.Git.ShouldEqual(new BuildGitInstructions(BuildGitInstructions.StudioManaged, "studio/invoicing/pay-invoice", "Build PayInvoice (backend)"));
    [Fact] void should_renew_with_the_order() => JsonNode.Parse(_studio.BodiesTo($"work/{given.an_order.SliceId}/lease").First())!["orderId"]!.GetValue<string>().ShouldEqual(given.an_order.OrderId);
    [Fact] void should_say_the_lease_holds() => _held.ShouldBeTrue();
    [Fact] void should_say_the_lease_is_lost_when_studio_answers_gone() => _lost.ShouldBeFalse();
    [Fact] void should_read_how_studio_settled() => _settled.ShouldEqual("built");
}
