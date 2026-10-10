// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_an_esm_v9_model : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => File.WriteAllBytes(_file, [.. PublicEventsCorpus.V9.SourceForms.Single().Documents.Single().Bytes]);
    async Task Because() => _result = await Plan();

    [Fact] void should_refuse_the_version_before_renderer_planning() => _result.Diagnostics.Select(_ => _.Code).ShouldContain(RenderedSemanticVersions.NotAdmittedCode);
    [Fact] void should_not_publish_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_not_report_source_errors() => _result.Diagnostics.Where(_ => _.Code.StartsWith("PLAY", StringComparison.Ordinal) && _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
}
