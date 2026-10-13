// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_checking_event_identities;

public class with_an_application_id_change : given.an_existing_event_publication
{
    void Establish() => PinEvent("OriginalProjectRegistered");

    async Task Because()
    {
        var plan = await Plan();
        _check = await _publisher.Check(Request(plan), CancellationToken.None);
        _error = await Catch.Exception(() => Publish(plan));
    }

    [Fact] void should_refuse_publication() => _error.ShouldBeOfExactType<UnsafeArtifactPublication>();
    [Fact] void should_report_the_event_identity_refusal() => ((UnsafeArtifactPublication)_error!).DiagnosticCode.ShouldEqual("CLI-RENDER-016");
    [Fact] void should_refuse_the_read_only_check() => _check.Refused.ShouldEqual(1);
    [Fact] void should_report_the_event_identity_code_in_the_receipt() => _check.Receipt.Changes.Single().Code.ShouldEqual("CLI-RENDER-016");
    [Fact] void should_name_the_changed_path() => _check.Receipt.Changes.Single().Reason!.ShouldContain(_eventPath);
    [Fact] void should_name_the_type() => _check.Receipt.Changes.Single().Reason!.ShouldContain("ProjectRegistered:");
    [Fact] void should_show_the_old_and_new_id() => _check.Receipt.Changes.Single().Reason!.ShouldContain("ProjectRegistered -> OriginalProjectRegistered");
    [Fact] void should_surface_the_stage_diagnostic() => _check.Receipt.Changes.Single().Reason!.ShouldContain("STAGE-EVENT-002: Event type");
    [Fact] void should_explain_the_pin_remedy() => _error!.Message.ShouldContain("Pin the event's 'id'");
    [Fact] void should_leave_every_destination_byte_unchanged() => DestinationIsUnchanged().ShouldBeTrue();
    [Fact] void should_create_no_control_state() => Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
}
