// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_the_generated_concept_rules_corpus : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;
    string _path = null!;

    async Task Establish()
    {
        _path = Path.Combine(_folder, "generated-concept-rules");
        foreach (var document in GeneratedConceptRulesCorpus.V20.SourceForms.Single(form => form.Name == "folder").Documents)
        {
            var path = Path.Combine(_path, document.DisplayPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllBytesAsync(path, document.Bytes.AsMemory());
        }
    }

    async Task Because() => _result = await _planning.Plan(
        new(_path, GeneratedConceptRulesCorpus.V20.ApplicationName, CratisRendering.TargetId, null, null),
        CancellationToken.None);

    [Fact] void should_compile_the_real_folder_form() => _result.Documents.ShouldEqual(GeneratedConceptRulesCorpus.V20.SourceForms.Single(form => form.Name == "folder").Documents.Length);
    [Fact] void should_refuse_the_corpus_constructs_stage_cannot_render() => _result.Success.ShouldBeFalse();
    [Fact] void should_have_no_publishable_artifacts() => (_result.Artifacts?.Artifacts.Any() != true).ShouldBeTrue();
    [Fact] void should_surface_an_error_diagnostic() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error);
    [Fact] void should_not_refuse_the_esm_version() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == RenderedSemanticVersions.NotAdmittedCode).ShouldBeFalse();
}
