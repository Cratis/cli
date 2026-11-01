// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_release_baseline;

public class and_scope_resolution_fails : given.a_release_baseline
{
    IRenderReleaseBaseline _check = null!;

    void Establish()
    {
        _check = Substitute.For<IRenderReleaseBaseline>();
        _request = _request with { Scope = ["Unknown"], ReleaseBaseline = _check };
    }

    void Because() => Plan();

    [Fact] void should_fail_planning() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_scope_error() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "CLI-RENDER-011");
    [Fact] void should_not_assess_the_baseline() => _check.DidNotReceive().Assess(Arg.Any<SemanticCompilation>());
}
