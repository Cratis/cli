// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_the_text_rules_corpus : given.a_planned_corpus_form
{
    ScreenplayRenderPlan _result = null!;

    /// <summary>
    /// TextRulesCorpus has no ESM vector, so v24 is planned from its source form and its bytes are not checked.
    /// </summary>
    /// <returns>The planning task.</returns>
    async Task Because() => _result = await PlanCorpusForm("TextRules", TextRulesCorpus.SourceForms.Single(form => form.Name == "first-class"));

    [Fact] void should_compile_the_corpus_documents() => _result.Documents.ShouldBeGreaterThan(0);
    [Fact] void should_not_refuse_the_esm_version() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == RenderedSemanticVersions.NotAdmittedCode).ShouldBeFalse();
    [Fact] void should_not_violate_the_renderer_contract() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == "CLI-RENDER-005").ShouldBeFalse();
    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
}
