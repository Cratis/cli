// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_scopes_are_checked : given.a_render_command
{
    int _result;

    void Establish()
    {
        _settings.Scope = ["Orders.Placing"];
        _settings.Check = true;
        _publication.Check(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ArtifactPublicationCheckResult(1, 0, 0, 0, false, new([], new(null, "next-manifest"))));
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_forward_the_scope_to_planning() => _planning.Received(1).Plan(Arg.Is<ScreenplayRenderRequest>(request => request.Scope.SequenceEqual(_settings.Scope)), Arg.Any<CancellationToken>());
    [Fact] void should_report_changes_pending() => _result.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_never_publish() => _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
    [Fact] void should_never_recover() => _publication.DidNotReceive().Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
}
