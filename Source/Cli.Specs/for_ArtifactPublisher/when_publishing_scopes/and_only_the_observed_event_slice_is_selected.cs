// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes;

public class and_only_the_observed_event_slice_is_selected : given.a_cross_slice_reducer
{
    ArtifactPublicationResult _result = null!;
    ArtifactPublicationCheckResult _check = null!;
    ScreenplayRenderPlan _scoped = null!;

    async Task Because()
    {
        _scoped = await Plan("Projects.Registration.RegisterProject");
        Assert.True(_scoped.Success, string.Join("; ", _scoped.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _check = await _publisher.Check(Request(_scoped), CancellationToken.None);
        _result = await Publish(_scoped);
    }

    [Fact] void should_record_separate_source_chains() => _owners.Count.ShouldEqual(2);
    [Fact] void should_exclude_the_foreign_reducer_from_the_plan() => _scoped.Artifacts!.Artifacts.Any(artifact => artifact.RelativePath == _reducerPath).ShouldBeFalse();
    [Fact] void should_keep_the_foreign_reducer() => File.ReadAllBytes(Path.Combine(_destination, _reducerPath)).SequenceEqual(_reducerBytes).ShouldBeTrue();
    [Fact] void should_keep_its_ownership() => ArtifactPublicationStorage.ReadManifest(_destination)!.Artifacts.Select(artifact => artifact.Path).ShouldContain(_reducerPath);
    [Fact] void should_remove_nothing() => _result.Removed.ShouldEqual(0);
    [Fact] void should_agree_with_check() => _check.Removed.ShouldEqual(0);
}
