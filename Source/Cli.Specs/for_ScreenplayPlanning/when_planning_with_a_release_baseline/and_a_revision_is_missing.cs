// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_release_baseline;

public class and_a_revision_is_missing : given.a_release_baseline
{
    void Establish()
    {
        var source = Source[..Source.IndexOf("    slice StateChange Observe", StringComparison.Ordinal)];
        var current = Workspace(source, SemanticIdentityCatalog.Empty(_workspace.IdentityCatalog.Application));
        _request = Request(current) with { ReleaseBaseline = new FileReleaseBaseline(_baseline) };
    }
    void Because() => Plan();

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_about_the_missing_revision() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-BASELINE-002");
}
