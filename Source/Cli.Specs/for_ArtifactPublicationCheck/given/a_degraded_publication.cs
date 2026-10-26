// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.for_ArtifactPublisher.given;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_ArtifactPublicationCheck.given;

public class a_degraded_publication : a_publication_check
{
    private protected async Task PlanDegraded(bool strict, bool unsafeGeneration = false)
    {
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call => AcceptancePlans.Degraded(call.Arg<ArtifactRenderRequest>(), unsafeGeneration));
        _planning = new(new SemanticModelCompiler(), new RenderTargetRoster([new CratisRenderTarget(planner)]));
        _plan = (await _planning.Plan(new(_file, "Projects", "cratis") { Strict = strict }, CancellationToken.None)).Artifacts!;
    }
}
