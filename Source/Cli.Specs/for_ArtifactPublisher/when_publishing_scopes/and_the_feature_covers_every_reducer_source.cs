// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes;

public class and_the_feature_covers_every_reducer_source : given.a_cross_slice_reducer
{
    ArtifactPublicationResult _result = null!;
    ArtifactPublicationCheckResult _check = null!;

    void Establish() => File.WriteAllText(_lookupFile, LookupSource);

    async Task Because()
    {
        var plan = await Plan("Projects.Registration");
        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _check = await _publisher.Check(Request(plan), CancellationToken.None);
        _result = await Publish(plan);
    }

    [Fact] void should_remove_the_stale_reducer() => File.Exists(Path.Combine(_destination, _reducerPath)).ShouldBeFalse();
    [Fact] void should_remove_it_from_the_manifest() => ArtifactPublicationStorage.ReadManifest(_destination)!.Artifacts.Select(artifact => artifact.Path).ShouldNotContain(_reducerPath);
    [Fact] void should_receipt_the_deletion() => _result.Receipt.Changes.Single(change => change.Path == _reducerPath).Kind.ShouldEqual("delete");
    [Fact] void should_check_the_same_deletion() => _check.Receipt.Changes.Single(change => change.Path == _reducerPath).Kind.ShouldEqual("delete");
}
