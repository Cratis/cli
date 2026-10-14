// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_no_event_expectations : given.a_screenplay_planning
{
    static readonly string NoEventsSource = string.Join('\n',
        "module Work",
        "  feature Items",
        "    slice StateChange Respond",
        "      command Echo",
        "        id String identifier",
        "        returns id",
        "      specification EmptyResponse",
        "        when Echo",
        "          id = \"one\"",
        "        then returns \"one\"",
        "        then no events");

    ScreenplayRenderPlan _result = null!;
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        File.WriteAllText(_file, NoEventsSource);
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("no-events"), "no-events", "NoEvents.play", NoEventsSource);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeTrue();
        _model = compilation.Value!.Model;
    }

    async Task Because() => _result = await Plan();

    [Fact] void should_compile_to_v17() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V17);
    [Fact] void should_admit_v17() => RenderedSemanticVersions.Check(_model.SemanticVersion).ShouldBeNull();
    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_report_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_render_the_no_event_assertion() => Encoding.UTF8.GetString(_result.Artifacts!.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("when_empty_response.cs", StringComparison.Ordinal)).Bytes.AsSpan()).ShouldContain("should_append_no_new_events() => _scenario.AppendedEvents.Count(entry => entry.Result.IsSuccess).ShouldEqual(0)");
}
