// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_the_screen_composition_corpus : Specification
{
    ScreenplayRenderPlan _result = null!;
    string _folder = null!;

    void Establish()
    {
        _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var form = ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder");
        foreach (var document in form.Documents)
        {
            var path = Path.Combine(_folder, document.DisplayPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, document.Bytes.AsSpan());
        }
    }

    async Task Because() => _result = await new ScreenplayPlanning().Plan(
        new(_folder, ScreenCompositionCorpus.V1.ApplicationName, CratisRendering.TargetId, null, null),
        CancellationToken.None);

    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_compile_the_real_folder_form() => _result.Documents.ShouldEqual(ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder").Documents.Length);
    [Fact] void should_not_report_the_previous_query_shape_or_spec_generation_refusals() => _result.Diagnostics.Any(_ =>
        string.Equals(_.Code, "STAGE-ESM-010", StringComparison.Ordinal) || string.Equals(_.Code, "STAGE-ESM-011", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_report_stage_errors_for_the_corpus() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_emit_the_no_argument_observable_collection_query() => ArtifactText.ShouldContain("AllWorkItems");
    [Fact] void should_emit_the_parameterized_observable_collection_query() => ArtifactText.ShouldContain("CommentsForWorkItem");
    [Fact] void should_emit_the_collection_query_parameter() => ArtifactText.ShouldContain("workItemId");
    [Fact] void should_emit_the_listing_query_specification() => ArtifactText.ShouldContain("when_listing_created_work_items_is_queried");
    [Fact] void should_emit_the_comments_query_specification() => ArtifactText.ShouldContain("Needs compact layout");
    [Fact] void should_emit_the_renamed_details_query_specification() => ArtifactText.ShouldContain("when_showing_renamed_details_is_queried");

    string ArtifactText => Encoding.UTF8.GetString([.. _result.Artifacts!.Artifacts.SelectMany(_ => _.Bytes)]);

    void Destroy()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
