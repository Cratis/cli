// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_axon_ignores_reducer_migration : for_AxonRenderTarget.given.an_axon_target
{
    ScreenplayRenderPlan _result = null!;

    void Establish() => _planning = new(new SemanticModelCompiler(), new RenderTargetRoster([_target]));

    async Task Because() => _result = await _planning.Plan(
        new(_file, "Projects", "axon")
        {
            MigrateReducers = ["Projects.Registration.ProjectLookup.ProjectSummary"]
        },
        CancellationToken.None);

    [Fact] void should_warn_instead_of_reading_a_cratis_destination() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == "CLI-RENDER-030").Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_not_resolve_migration_evidence() => _input.ShouldNotBeNull();
    [Fact] void should_not_report_a_cratis_migration_refusal() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == CratisReducerMigration.RefusalCode).ShouldBeFalse();
}
