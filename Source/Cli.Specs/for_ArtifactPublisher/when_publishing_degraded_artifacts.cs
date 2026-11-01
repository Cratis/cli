// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_publishing_degraded_artifacts : for_ArtifactPublicationCheck.given.a_degraded_publication
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_publish_safe_artifacts_independently_of_acceptance(bool strict)
    {
        await PlanDegraded(strict);
        _plan.Success.ShouldEqual(!strict);
        _plan.CanPublishArtifacts.ShouldBeTrue();

        var result = await Publish(cancellationToken: TestContext.Current.CancellationToken);

        result.Written.ShouldEqual(_plan.Artifacts.Length);
        foreach (var artifact in _plan.Artifacts)
        {
            (await File.ReadAllBytesAsync(ArtifactPath(artifact.RelativePath), TestContext.Current.CancellationToken)).ShouldEqual<byte>([.. artifact.Bytes]);
        }
        File.Exists(ArtifactPublicationStorage.ManifestPath(_destination)).ShouldBeTrue();
    }
}
