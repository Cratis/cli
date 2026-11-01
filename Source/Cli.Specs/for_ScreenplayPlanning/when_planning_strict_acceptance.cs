// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_strict_acceptance : given.a_screenplay_planning
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_forward_acceptance_through_the_path_to_the_real_profile(bool strict)
    {
        ArtifactRenderRequest captured = null!;
        var planner = Substitute.For<IArtifactRenderPlanner>();
        planner.Plan(Arg.Any<ArtifactRenderRequest>()).Returns(call =>
        {
            captured = call.Arg<ArtifactRenderRequest>();
            return new CratisArtifactRenderPlanner().Plan(captured);
        });
        _planning = new(new SemanticModelCompiler(), new RenderTargetRoster([new CratisRenderTarget(planner)]));

        var result = await _planning.Plan(new(_file, "Projects", "cratis") { Strict = strict }, CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.Artifacts!.Strict.ShouldEqual(strict);
        var input = captured.Profile.Inputs.SingleOrDefault(input => input.Name == StrictAcceptanceInput.Name);
        if (strict)
        {
            StrictAcceptanceInput.TryRead(input!, out var enabled).ShouldBeTrue();
            enabled.ShouldBeTrue();
        }
        else
        {
            input.ShouldBeNull();
            var legacy = await Plan();
            result.Artifacts.Digest.ShouldEqual(legacy.Artifacts!.Digest);
        }
        result.ScaffoldOptions!.Strict.ShouldEqual(strict);
        var scaffold = CratisRendering.PlanScaffold(result.ScaffoldOptions);
        scaffold.Success.ShouldBeTrue();
    }
}
