// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes;

#pragma warning disable MA0136 // Fixture line endings do not affect normalized rendered artifacts.

public class and_an_empty_scope_has_an_unsupported_sibling : given.a_model_with_an_unsupported_projection
{
    ScreenplayRenderPlan[] _results = null!;

    void Establish() => File.WriteAllText(_taskFile, """
        module Tasks
          feature Backlog
            feature Intake
              description "Tasks have been removed"
        """);

    async Task Because() => _results = [await Plan("Tasks"), await Plan("Tasks.Backlog.Intake")];

    [Fact] void should_admit_the_empty_scopes() => _results.All(result => result.Success).ShouldBeTrue();
    [Fact] void should_report_the_unsupported_sibling_as_a_warning() => _results.All(result => result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAN-UNSUPPORTEDEVENTCONTEXT" && diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning)).ShouldBeTrue();
    [Fact] void should_not_report_invalid_slice_selection() => _results.SelectMany(result => result.Diagnostics).Where(diagnostic => diagnostic.Code == "PLAN-INVALIDSLICESELECTION").ShouldBeEmpty();
    [Fact] void should_have_no_slice_artifacts() => _results.All(result => result.Artifacts!.Artifacts.IsEmpty).ShouldBeTrue();
}
