// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes;

public class and_disjoint_scopes_precede_a_full_render : for_ScreenplayPlanning.when_planning_scopes.given.a_scoped_model
{
    ArtifactPublicationResult _second = null!;
    ArtifactPublicationResult _full = null!;
    ScreenplayRenderPlan _plan = null!;
    byte[] _program = null!;

    async Task Establish()
    {
        await Publish(await Plan("Tasks"));
        _program = File.ReadAllBytes(Path.Combine(_destination, "Program.cs"));
    }

    async Task Because()
    {
        _second = await Publish(await Plan("Projects.Registration"));
        _plan = await Plan();
        _full = await Publish(_plan);
    }

    [Fact] void should_not_remove_other_scopes() => _second.Removed.ShouldEqual(0);
    [Fact] void should_keep_all_scoped_artifacts_on_full_render() => _full.Removed.ShouldEqual(0);
    [Fact] void should_own_the_entire_application() => ArtifactPublicationStorage.ReadManifest(_destination)!.Artifacts.Select(artifact => artifact.Path).ShouldContainOnly(_plan.Artifacts!.Artifacts.Select(artifact => artifact.RelativePath));
    [Fact] void should_preserve_scaffold_bytes() => File.ReadAllBytes(Path.Combine(_destination, "Program.cs")).SequenceEqual(_program).ShouldBeTrue();
    [Fact] void should_keep_unscoped_receipts_unchanged() => ArtifactPublicationStorage.Serialize(_full.Receipt).Contains("\"scope\"", StringComparison.Ordinal).ShouldBeFalse();
}
