// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_an_esm_v10_model : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => File.WriteAllBytes(_file, [.. ReactionIdentityCorpus.V10.SourceForms.Single(form => form.Name == "single").Documents.Single().Bytes]);
    async Task Because() => _result = await Plan();

    [Fact] void should_admit_the_execution_identity_construct() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_not_refuse_the_version() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(RenderedSemanticVersions.NotAdmittedCode);
    [Fact] void should_plan_artifacts() => _result.Artifacts!.Artifacts.ShouldNotBeEmpty();
    [Fact] void should_not_report_source_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Code.StartsWith("PLAY", StringComparison.Ordinal) && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
}
