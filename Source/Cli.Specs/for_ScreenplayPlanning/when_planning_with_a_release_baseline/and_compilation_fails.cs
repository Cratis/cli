// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_release_baseline;

public class and_compilation_fails : given.a_release_baseline
{
    IRenderReleaseBaseline _check = null!;

    void Establish()
    {
        _check = Substitute.For<IRenderReleaseBaseline>();
        var catalog = _workspace.IdentityCatalog;
        _request = _request with
        {
            Documents = SemanticDocumentSet.Create([SemanticSourceDocument.Create(catalog.ResolveDocument("source"), "source", "source.play", "not a screenplay")], catalog),
            ReleaseBaseline = _check
        };
    }

    void Because() => Plan();

    [Fact] void should_fail_planning() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_assess_the_baseline() => _check.DidNotReceive().Assess(Arg.Any<SemanticCompilation>());
    [Fact] void should_not_report_baseline_warnings() => _result.Diagnostics.Where(diagnostic => diagnostic.Code.StartsWith("STAGE-BASELINE-", StringComparison.Ordinal)).ShouldBeEmpty();
}
