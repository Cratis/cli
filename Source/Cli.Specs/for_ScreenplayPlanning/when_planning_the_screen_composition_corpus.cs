// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_the_screen_composition_corpus : Specification
{
    ScreenplayRenderPlan _result = null!;
    JsonDocument _scene = null!;
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

    async Task Because()
    {
        _result = await new ScreenplayPlanning().Plan(
            new(_folder, ScreenCompositionCorpus.V1.ApplicationName, CratisRendering.TargetId, null, null),
            CancellationToken.None);
        _scene = JsonDocument.Parse(SceneText);
    }

    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_compile_the_real_folder_form() => _result.Documents.ShouldEqual(ScreenCompositionCorpus.V1.SourceForms.Single(_ => _.Name == "folder").Documents.Length);
    [Fact] void should_not_report_the_previous_query_shape_or_spec_generation_refusals() => _result.Diagnostics.Any(_ =>
        string.Equals(_.Code, "STAGE-ESM-010", StringComparison.Ordinal) || string.Equals(_.Code, "STAGE-ESM-011", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_not_report_stage_errors_for_the_corpus() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_report_the_guarded_action_refusal_without_blocking_the_safe_scene() => _result.Diagnostics.Any(_ => _.Code == "STAGE-SCENE-ACTION-001" && _.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_emit_the_authored_ui_profile_packages() => SceneText.ShouldContain("\"packages\":[\"core\",\"Cratis.Components\"]");
    [Fact] void should_emit_the_authored_application_layout() => Names("layouts").ShouldContain("AppShell");
    [Fact] void should_emit_the_authored_master_detail_template() => Names("screenTemplates").ShouldContain("MasterDetail");
    [Fact] void should_emit_the_authored_dialog_template() => Names("dialogTemplates").ShouldContain("EditDialog");
    [Fact] void should_emit_the_authored_screen_set() => Names("screens").Order(StringComparer.Ordinal).ShouldContainOnly(["CommentThread", "WorkItemDetails", "WorkItemList"]);
    [Fact] void should_emit_the_authored_navigation_contribution() => SceneText.ShouldContain("\"contributionPointName\":\"Navigation\"");
    [Fact] void should_emit_the_authored_template_slots() => SceneText.ShouldContain("\"slots\":[{\"name\":\"list\"},{\"name\":\"details\"}]");
    [Fact] void should_emit_the_create_work_item_form() => SceneText.ShouldContain("CreateWorkItemForm");
    [Fact] void should_emit_the_rename_work_item_form() => SceneText.ShouldContain("RenameWorkItemForm");
    [Fact] void should_not_emit_guarded_action_alternatives() => SceneText.ShouldNotContain("\"alternatives\"");
    [Fact] void should_not_emit_guarded_action_fallbacks() => SceneText.ShouldNotContain("\"otherwise\"");
    [Fact] void should_not_replace_the_authored_scene_with_the_generated_fallback() => Names("screens").ShouldNotContain("ScreenComposition");
    [Fact] void should_emit_the_no_argument_observable_collection_query() => ArtifactText.ShouldContain("AllWorkItems");
    [Fact] void should_emit_the_parameterized_observable_collection_query() => ArtifactText.ShouldContain("CommentsForWorkItem");
    [Fact] void should_emit_the_collection_query_parameter() => ArtifactText.ShouldContain("workItemId");
    [Fact] void should_emit_the_listing_query_specification() => ArtifactText.ShouldContain("when_listing_created_work_items_is_queried");
    [Fact] void should_emit_the_comments_query_specification() => ArtifactText.ShouldContain("Needs compact layout");
    [Fact] void should_emit_the_renamed_details_query_specification() => ArtifactText.ShouldContain("when_showing_renamed_details_is_queried");

    string ArtifactText => Encoding.UTF8.GetString([.. _result.Artifacts!.Artifacts.SelectMany(_ => _.Bytes)]);
    string SceneText => Encoding.UTF8.GetString(_result.Artifacts!.Artifacts.Single(_ => _.RelativePath == "scene.json").Bytes.AsSpan());
    IEnumerable<string> Names(string collection) => _scene.RootElement.GetProperty(collection).EnumerateArray().Select(_ => _.GetProperty("name").GetString()!);

    void Destroy()
    {
        _scene?.Dispose();
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }
}
