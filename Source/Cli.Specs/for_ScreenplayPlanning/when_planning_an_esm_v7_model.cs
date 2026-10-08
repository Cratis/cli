// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_an_esm_v7_model : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;
    string _path = null!;

    void Establish()
    {
        var form = RegisterProjectCorpus.V7.SourceForms.Single(_ => _.Name == "folder");
        _path = Path.Combine(_folder, "v7");
        foreach (var document in form.Documents)
        {
            var path = Path.Combine(_path, document.DisplayPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, document.Bytes.AsSpan());
        }
    }

    async Task Because() => _result = await Plan(_path);

    [Fact] void should_not_be_successful() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_plan_any_publishable_artifacts() => _result.Artifacts!.Artifacts.ShouldBeEmpty();
    [Fact] void should_leave_version_admission_to_the_renderer() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain(RenderedSemanticVersions.NotAdmittedCode);
    [Fact] void should_report_the_renderers_generated_value_and_response_refusals() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).Select(_ => _.Code).Distinct().Order(StringComparer.Ordinal).ShouldEqual("STAGE-ESM-028", "STAGE-ESM-029");
    [Fact] void should_compile_without_source_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error && _.Code.StartsWith("PLAY", StringComparison.Ordinal)).ShouldBeEmpty();
}
