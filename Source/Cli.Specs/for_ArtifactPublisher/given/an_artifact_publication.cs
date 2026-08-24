// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_ArtifactPublisher.given;

public class an_artifact_publication : for_ScreenplayPlanning.given.a_screenplay_planning
{
    protected string _destination = null!;
    private protected ArtifactRenderPlan _plan = null!;
    private protected ArtifactPublisher _publisher = null!;

    async Task Establish()
    {
        _destination = Path.Combine(_folder, "out");
        _plan = (await Plan()).Artifacts!;
        _publisher = new ArtifactPublisher();
    }

    private protected Task<ArtifactPublicationResult> Publish(
        bool force = false,
        CancellationToken cancellationToken = default) =>
        _publisher.Publish(new(_plan, _destination, force), cancellationToken);

    protected string ArtifactPath(string relativePath) =>
        ArtifactPublicationStorage.ArtifactPath(_destination, relativePath);

    protected string FirstSourcePath() =>
        ArtifactPath(_plan.Artifacts.First(_ => _.RelativePath.EndsWith(".cs", StringComparison.Ordinal)).RelativePath);
}
