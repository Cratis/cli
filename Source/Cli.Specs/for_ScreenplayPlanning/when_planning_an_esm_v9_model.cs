// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_an_esm_v9_model : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => File.WriteAllBytes(_file, [.. PublicEventsCorpus.V9.SourceForms.Single().Documents.Single().Bytes]);
    async Task Because() => _result = await Plan();

    [Fact] void should_not_refuse_the_version() => _result.Diagnostics.Select(_ => _.Code).ShouldNotContain(RenderedSemanticVersions.NotAdmittedCode);
    [Fact] void should_report_no_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).Select(_ => $"{_.Code}: {_.Message}").ShouldBeEmpty();
    [Fact] void should_render_the_published_event_as_public() => Text("Integration/Translation/PublishOrderShipped/PublishOrderShipped.Events.cs").ShouldContain("[Public]");
    [Fact] void should_render_the_outbound_translation() => _result.Artifacts!.Artifacts.Select(_ => _.RelativePath).ShouldContain("Integration/Translation/PublishOrderShipped/OrderShippedPublisher.cs");
    [Fact] void should_not_report_source_errors() => _result.Diagnostics.Where(_ => _.Code.StartsWith("PLAY", StringComparison.Ordinal) && _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();

    string Text(string path) => Encoding.UTF8.GetString(_result.Artifacts!.Artifacts.Single(_ => _.RelativePath == path).Bytes.AsSpan());
}
