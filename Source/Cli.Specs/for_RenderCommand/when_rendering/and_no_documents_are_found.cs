// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_no_documents_are_found : given.a_render_command
{
    int _result;

    void Establish() =>
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ScreenplayRenderPlan(0, [], null));

    async Task Because() => _result = await Execute();

    [Fact] void should_report_that_nothing_was_found() => _result.ShouldEqual(ExitCodes.NotFound);
    [Fact] void should_not_publish_anything() => _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
}
