// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_carrying_semantic_exclusions;

public class from_documents : given.an_excluded_slice
{
    ScreenplayRenderPlan _result = null!;

    void Because() => _result = _planning.PlanDocuments(new(_documents, "Projects", "cratis"), CancellationToken.None);

    [Fact] void should_plan_the_surviving_model() => _result.Success.ShouldBeTrue();
    [Fact] void should_forward_exact_compiler_entries() => AssertExclusions(_requests.Single().Exclusions, _expected.Exclusions);
    [Fact] void should_not_invent_an_excluded_leaf_identity() => _result.SemanticAddresses.Sources.Any(source => source.Address == "Projects/Registration/Removed").ShouldBeFalse();
    [Fact] void should_render_the_surviving_command() => _result.Artifacts.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/RegisterProject.cs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_excluded_errors_out_of_planning_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
}
