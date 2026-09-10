// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_rerendering_an_unchanged_plan : given.an_artifact_publication
{
    ArtifactPublicationResult _result = null!;
    IReadOnlyList<string> _before = null!;
    IReadOnlyList<string> _after = null!;
    byte[] _manifestBefore = null!;
    byte[] _manifestAfter = null!;

    async Task Because()
    {
        await Publish();
        _before = Hashes();
        _manifestBefore = await File.ReadAllBytesAsync(ArtifactPublicationStorage.ManifestPath(_destination));
        _result = await Publish();
        _after = Hashes();
        _manifestAfter = await File.ReadAllBytesAsync(ArtifactPublicationStorage.ManifestPath(_destination));
    }

    [Fact] void should_write_nothing() => _result.Written.ShouldEqual(0);
    [Fact] void should_remove_nothing() => _result.Removed.ShouldEqual(0);
    [Fact] void should_count_every_artifact_as_unchanged() => _result.Unchanged.ShouldEqual(_plan.Artifacts.Length);
    [Fact] void should_leave_every_managed_byte_unchanged() => _after.ShouldContainOnly(_before);
    [Fact] void should_leave_the_manifest_bytes_unchanged() => _manifestAfter.SequenceEqual(_manifestBefore).ShouldBeTrue();

    IReadOnlyList<string> Hashes() =>
        [.. _plan.Artifacts.Select(_ => ArtifactPublicationStorage.Hash(ArtifactPath(_.RelativePath)))];
}
