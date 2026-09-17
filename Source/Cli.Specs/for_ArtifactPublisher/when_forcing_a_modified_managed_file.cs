// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_forcing_a_modified_managed_file : given.an_artifact_publication
{
    ArtifactPublicationResult _result = null!;
    string _modifiedHash = null!;

    async Task Establish()
    {
        await Publish();
        await File.WriteAllTextAsync(FirstSourcePath(), "user modified");
        _modifiedHash = ArtifactPublicationStorage.Hash(FirstSourcePath());
    }

    async Task Because() => _result = await Publish(force: true);

    [Fact] void should_report_one_change() => _result.Receipt.Changes.Count.ShouldEqual(1);
    [Fact] void should_report_the_actual_forced_over_bytes() => _result.Receipt.Changes.Single().BeforeSha256.ShouldEqual(_modifiedHash);
    [Fact] void should_report_the_planned_after_bytes() => _result.Receipt.Changes.Single().AfterSha256.ShouldEqual(ArtifactPublicationStorage.Hash(FirstSourcePath()));
    [Fact] void should_distinguish_actual_before_from_planned_bytes() => _result.Receipt.Changes.Single().BeforeSha256.ShouldNotEqual(_result.Receipt.Changes.Single().AfterSha256);
    [Fact] void should_replace_the_modified_managed_file() => ArtifactPublicationStorage.Hash(FirstSourcePath()).ShouldEqual(_plan.Artifacts.Single(_ => ArtifactPath(_.RelativePath) == FirstSourcePath()).Sha256);
    [Fact] void should_report_one_written_artifact() => _result.Written.ShouldEqual(1);
}
