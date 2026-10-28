// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Cli.for_ArtifactPublisher.given;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_ArtifactPublisher.when_checking_event_identities;

public class with_degraded_acceptance : given.an_existing_event_publication
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_still_refuse_a_changed_stored_event_identity_without_writing(bool strict)
    {
        PinEvent("OriginalProjectRegistered");
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call => AcceptancePlans.Degraded(call.Arg<ArtifactRenderRequest>()));
        _planning = new(new SemanticModelCompiler(), new RenderTargetRoster([new CratisRenderTarget(planner)]));
        var plan = await _planning.Plan(new(_folder, "Shop", "cratis") { Strict = strict }, CancellationToken.None);
        plan.Artifacts!.CanPublishArtifacts.ShouldBeTrue();

        var result = await _publisher.Check(Request(plan), CancellationToken.None);
        result.Refused.ShouldBeGreaterThan(0);
        DestinationIsUnchanged().ShouldBeTrue();
        var error = await Catch.Exception(() => Publish(plan));

        ((UnsafeArtifactPublication)error!).DiagnosticCode.ShouldEqual("CLI-RENDER-016");
        DestinationIsUnchanged().ShouldBeTrue();
        Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
    }
}
