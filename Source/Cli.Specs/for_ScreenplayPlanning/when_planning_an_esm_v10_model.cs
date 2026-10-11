// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_an_esm_v10_model : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => File.WriteAllBytes(_file, [.. ReactionIdentityCorpus.V10.SourceForms.Single(form => form.Name == "single").Documents.Single().Bytes]);
    async Task Because() => _result = await Plan();

    [Fact] void should_refuse_the_version_before_renderer_planning() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldContainOnly(RenderedSemanticVersions.NotAdmittedCode);
    [Fact] void should_name_the_refused_version() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == RenderedSemanticVersions.NotAdmittedCode).Message.ShouldContain("ESM v10.0");
    [Fact] void should_not_plan_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_not_report_source_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Code.StartsWith("PLAY", StringComparison.Ordinal) && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
}
