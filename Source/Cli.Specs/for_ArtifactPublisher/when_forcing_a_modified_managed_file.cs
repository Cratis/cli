// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_forcing_a_modified_managed_file : given.an_artifact_publication
{
    ArtifactPublicationResult _result = null!;

    async Task Establish()
    {
        await Publish();
        await File.WriteAllTextAsync(FirstSourcePath(), "user modified");
    }

    async Task Because() => _result = await Publish(force: true);

    [Fact] void should_replace_the_modified_managed_file() => ArtifactPublicationStorage.Hash(FirstSourcePath()).ShouldEqual(_plan.Artifacts.Single(_ => ArtifactPath(_.RelativePath) == FirstSourcePath()).Sha256);
    [Fact] void should_report_one_written_artifact() => _result.Written.ShouldEqual(1);
}
