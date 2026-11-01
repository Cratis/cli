// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_model_root;

public class with_a_scope_outside_the_baseline_violation : given.a_persisted_model_root
{
    void Establish()
    {
        ReleaseBaselineCommands.Admit(_root, "v1.0").ExitCode.ShouldEqual(0);
        File.WriteAllText(_document, ModelSource.Replace("value String", "value Int", StringComparison.Ordinal));
    }

    async Task Because() => _result = await _planning.Plan(new(_root, "Model", "cratis") { Scope = ["M.F.Observe"] }, CancellationToken.None);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_about_the_unselected_event() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-BASELINE-001" && diagnostic.Message.Contains("Recorded", StringComparison.Ordinal));
    [Fact] void should_assess_the_rendered_revision() => _requests.Single().ReleaseBaseline!.ModelRevision.ShouldEqual(_result.Artifacts!.SemanticRevision);
    [Fact] void should_not_render_the_unselected_event() => _result.Artifacts!.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/Record.cs", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_render_the_selected_event() => _result.Artifacts!.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("/Observe.cs", StringComparison.Ordinal)).ShouldBeTrue();
}
