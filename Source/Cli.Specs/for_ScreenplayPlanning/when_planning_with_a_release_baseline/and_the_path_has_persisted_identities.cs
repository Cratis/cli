// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_release_baseline;

public class and_the_path_has_persisted_identities : when_planning_a_model_root.given.a_persisted_model_root
{
    ScreenplayWorkspace _workspace = null!;
    string[] _metadataBefore = null!;
    string[] _metadataAfter = null!;

    void Establish()
    {
        ReleaseBaselineCommands.Admit(_root, "v1.0").ExitCode.ShouldEqual(0);
        File.WriteAllText(_document, ModelSource.Replace("value String", "value Int", StringComparison.Ordinal));
        _workspace = OpenWorkspace();
        _metadataBefore = Metadata();
    }

    async Task Because()
    {
        _result = await _planning.Plan(new(_root, "Model", "cratis"), CancellationToken.None);
        _metadataAfter = Metadata();
    }

    string[] Metadata() => [.. Directory.EnumerateFiles(Path.Combine(_root, ".screenplay"), "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).Select(path => $"{Path.GetRelativePath(_root, path)}:{Convert.ToHexString(File.ReadAllBytes(path))}")];

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_real_persisted_identity_violation() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-BASELINE-001");
    [Fact] void should_assess_the_opened_revision() => _requests.Single().ReleaseBaseline!.ModelRevision.ShouldEqual(_workspace.Compilation.Value!.Model.Revision);
    [Fact] void should_render_the_assessed_revision() => _result.Artifacts!.SemanticRevision.ShouldEqual(_requests.Single().ReleaseBaseline!.ModelRevision);
    [Fact] void should_keep_the_actual_violation_contract() => _requests.Single().ReleaseBaseline!.Violations.Single().ContractId.ShouldEqual(Recorded(_workspace.Compilation.Value!.Model.Application).ContractId);
    [Fact] void should_not_write_metadata() => _metadataAfter.ShouldEqual(_metadataBefore);
}
