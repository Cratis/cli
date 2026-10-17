// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_the_generation_fixtures_corpus : given.a_planned_corpus_form
{
    ScreenplayRenderPlan _result = null!;

    async Task Because() => _result = await PlanCorpusForm("GenerationFixtures", GenerationFixturesCorpus.SourceForms.Single(form => form.Name == "single"));

    [Fact] void should_compile_the_corpus_documents() => _result.Documents.ShouldBeGreaterThan(0);
    [Fact] void should_not_refuse_the_esm_version() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == RenderedSemanticVersions.NotAdmittedCode).ShouldBeFalse();
    [Fact] void should_not_violate_the_renderer_contract() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "CLI-RENDER-005").ShouldBeFalse();
    [Fact] void should_fail_planning() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_pinned_value_refusal() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-011").ShouldBeTrue();
    [Fact] void should_report_the_unsupported_shape_refusal() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-019").ShouldBeTrue();
    [Fact] void should_not_report_the_unsupported_generated_type_refusal() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "STAGE-ESM-028").ShouldBeFalse();
}
