// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_checking_event_identities;

public class with_an_unreadable_application_id : given.an_existing_event_publication
{
    void Establish() => File.WriteAllText(Path.Combine(_destination, _eventPath),
        "namespace Shop.Projects.Registration.RegisterProject; [EventType(StoredId)] public record ProjectRegistered;");

    async Task Because()
    {
        var plan = await Plan();
        var request = Request(plan) with { Force = true };
        _check = await _publisher.Check(request, CancellationToken.None);
        _error = await Catch.Exception(() => _publisher.Publish(request, CancellationToken.None));
    }

    [Fact] void should_refuse_publication_even_with_force() => _error.ShouldBeOfExactType<UnsafeArtifactPublication>();
    [Fact] void should_report_the_event_identity_refusal() => ((UnsafeArtifactPublication)_error!).DiagnosticCode.ShouldEqual("CLI-RENDER-016");
    [Fact] void should_explain_the_unreadable_id() => _check.Receipt.Changes.Single().Reason.ShouldContain("STAGE-EVENT-002: Event type");
    [Fact] void should_name_the_unreadable_expression() => _error!.Message.ShouldContain("StoredId -> ProjectRegistered");
    [Fact] void should_leave_the_unreadable_file_unchanged() => File.ReadAllText(Path.Combine(_destination, _eventPath)).ShouldContain("[EventType(StoredId)]");
    [Fact] void should_create_no_control_state() => Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
}
