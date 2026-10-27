// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_receiving_cratis_only_inputs : given.an_axon_target
{
    void Because()
    {
        var addresses = SemanticAddressIndex.From(_compilation.Model.Application, true);
        var readModel = addresses.Sources.Single(source => source.Kind == "readmodel");
        var input = ArtifactRenderInput.Create("test", "1", [1]);
        var migration = ReducerMigrationEvidence.FromExistingFile(addresses.Identity(readModel.Id), "Reducer.cs", "namespace Projects; public class Reducer : IReducerFor<ProjectSummary> { public ProjectSummary Reduce(ProjectRegistered e, ProjectSummary? current) => new(); }");
        _target.Plan(_compilation, _execution, null, null, [], [], [], [], _warnings, renderingInput: input, migrations: [migration]);
    }

    [Fact] void should_warn_for_each_ignored_input() => _warnings.Count(warning => warning.Code == "CLI-RENDER-030").ShouldEqual(2);
    [Fact] void should_identify_the_ignored_inputs() => _warnings.Single(warning => warning.Message.Contains("rendering input", StringComparison.Ordinal)).Code.ShouldEqual("CLI-RENDER-030");
    [Fact] void should_keep_them_as_warnings() => _warnings.TrueForAll(warning => warning.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_not_emit_a_cratis_scaffold() => _target.PlanScaffold(new("Projects", "Projects", "Projects")).ShouldBeNull();
}
