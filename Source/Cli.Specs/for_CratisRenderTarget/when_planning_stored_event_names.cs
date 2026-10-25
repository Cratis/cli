// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_CratisRenderTarget;

public class when_planning_stored_event_names : given.a_cratis_render_target
{
    SemanticCompilation _compilation = null!;
    ArtifactRenderPlan _plan = null!;
    string _code = null!;

    void Because()
    {
        var source = StoredEventNamesCorpus.V16.SourceForms.Single(_ => _.Name == "single").Documents.Single().Text;
        var (compilation, executionPlan) = Compile(source, "StoredEventNames");
        _compilation = compilation;
        _plan = _target.Plan(compilation, executionPlan, null, null, [], [], [], [], []);
        _code = string.Join('\n', _plan.Artifacts.Where(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .Select(_ => Encoding.UTF8.GetString(_.Bytes.AsSpan())));
    }

    [Fact] void should_compile_to_v16() => _compilation.Model.SemanticVersion.ShouldEqual(SemanticVersion.V16);
    [Fact] void should_admit_v16() => RenderedSemanticVersions.Check(_compilation.Model.SemanticVersion).ShouldBeNull();
    [Fact] void should_plan_successfully() => _plan.Success.ShouldBeTrue();
    [Fact] void should_store_the_renamed_event_under_its_pinned_name() => _code.ShouldContain("[EventType(\"Original\")]\npublic record Renamed(");
    [Fact] void should_keep_unpinned_events_argument_free() => _code.ShouldContain("[EventType]\npublic record Recorded(");
}
