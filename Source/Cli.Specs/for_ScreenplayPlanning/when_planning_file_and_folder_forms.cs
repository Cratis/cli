// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_file_and_folder_forms : given.a_screenplay_planning
{
    ScreenplayRenderPlan _filePlan = null!;
    ScreenplayRenderPlan _folderPlan = null!;

    async Task Because()
    {
        _filePlan = await Plan();
        _folderPlan = await Plan(_folder);
    }

    [Fact] void should_plan_both_forms_successfully() => new[] { _filePlan, _folderPlan }.All(_ => _.Success).ShouldBeTrue();
    [Fact] void should_have_the_same_semantic_revision() => _folderPlan.Artifacts!.SemanticRevision.ShouldEqual(_filePlan.Artifacts!.SemanticRevision);
    [Fact] void should_have_the_same_artifact_paths() => _folderPlan.Artifacts!.Artifacts.Select(_ => _.RelativePath).SequenceEqual(_filePlan.Artifacts!.Artifacts.Select(_ => _.RelativePath)).ShouldBeTrue();
    [Fact] void should_have_the_same_artifact_hashes() => _folderPlan.Artifacts!.Artifacts.Select(_ => _.Sha256).SequenceEqual(_filePlan.Artifacts!.Artifacts.Select(_ => _.Sha256)).ShouldBeTrue();
    [Fact] void should_have_the_same_artifact_bytes() => _folderPlan.Artifacts!.Artifacts.Zip(_filePlan.Artifacts!.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
}
