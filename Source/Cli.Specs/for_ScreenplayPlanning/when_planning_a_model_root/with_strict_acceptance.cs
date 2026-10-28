// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_model_root;

public class with_strict_acceptance : given.a_persisted_model_root
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_forward_acceptance_from_the_persisted_root_to_stage(bool strict)
    {
        var result = await _planning.Plan(new(_root, "Model", "cratis") { Strict = strict }, CancellationToken.None);

        result.Success.ShouldBeTrue();
        result.Artifacts!.Strict.ShouldEqual(strict);
        var input = _requests.Single().Profile.Inputs.SingleOrDefault(input => input.Name == StrictAcceptanceInput.Name);
        if (strict)
        {
            StrictAcceptanceInput.TryRead(input!, out var enabled).ShouldBeTrue();
            enabled.ShouldBeTrue();
        }
        else
        {
            input.ShouldBeNull();
        }
        result.ScaffoldOptions!.Strict.ShouldEqual(strict);
        _compiler.DidNotReceive().Compile(Arg.Any<string>(), Arg.Any<SemanticDocumentSet>());
    }
}
