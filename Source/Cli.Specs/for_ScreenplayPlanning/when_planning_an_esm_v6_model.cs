// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_an_esm_v6_model : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;
    string _path = null!;

    void Establish()
    {
        var form = ReactionsCorpus.V6.SourceForms.Single(_ => _.Name == "single");
        _path = Path.Combine(_folder, "v6");
        foreach (var document in form.Documents)
        {
            var path = Path.Combine(_path, document.DisplayPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, document.Bytes.AsSpan());
        }
    }

    async Task Because() => _result = await Plan(_path);

    [Fact] void should_leave_admission_to_the_renderer() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain(RenderedSemanticVersions.NotAdmittedCode);
    [Fact] void should_compile_without_source_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error && _.Code.StartsWith("PLAY", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_reach_the_renderer_and_be_refused_there() => _result.Diagnostics.Select(_ => _.Code).ShouldContain("STAGE-ESM-016");
}
