// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_with_a_release_baseline;

public class and_a_workspace_revision_changed : given.a_release_baseline
{
    void Establish() => _request = Changed(Source.Replace("value String", "value Int", StringComparison.Ordinal));
    void Because() => Plan();

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_about_the_changed_revision() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-BASELINE-001" && diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning);
    [Fact] void should_keep_the_artifact_paths_and_hashes() => _result.Artifacts!.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)).ShouldEqual(_withoutBaseline.Artifacts!.Artifacts.Select(artifact => (artifact.RelativePath, artifact.Sha256)));
    [Fact] void should_keep_the_artifact_bytes() => _result.Artifacts!.Artifacts.Zip(_withoutBaseline.Artifacts!.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
}
