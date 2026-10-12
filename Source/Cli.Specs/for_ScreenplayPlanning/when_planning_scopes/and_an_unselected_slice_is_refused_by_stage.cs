// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes;

#pragma warning disable MA0136 // Fixture line endings do not affect normalized rendered artifacts.

public class and_an_unselected_slice_is_refused_by_stage : given.a_scoped_model
{
    ScreenplayRenderPlan _full = null!;
    ScreenplayRenderPlan _scoped = null!;

    async Task Establish()
    {
        await File.WriteAllTextAsync(_taskFile, """
            module Tasks
              feature Unsupported
                slice Translate ImportOtherProject
                  direction inbound
            """);
        _full = await Plan();
    }

    async Task Because() => _scoped = await Plan("Projects.Registration.RegisterProject");

    [Fact] void should_refuse_the_full_plan() => _full.Success.ShouldBeFalse();
    [Fact] void should_report_the_translation_direction_renderer_refusal() => _full.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error && diagnostic.Code == "STAGE-ESM-024");
    [Fact] void should_admit_the_independent_selected_slice() => _scoped.Success.ShouldBeTrue();
    [Fact] void should_have_no_scoped_error_diagnostics() => _scoped.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_include_the_selected_slice() => _scoped.Artifacts!.Artifacts.ShouldContain(artifact => artifact.RelativePath == "Projects/Registration/RegisterProject/RegisterProject.cs");
    [Fact] void should_exclude_the_refused_slice() => _scoped.Artifacts!.Artifacts.Any(artifact => artifact.RelativePath.Contains("ImportOtherProject", StringComparison.Ordinal)).ShouldBeFalse();
}
