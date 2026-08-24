// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_the_document_renders : given.a_render_command
{
    int _result;

    void Establish() => _settings.Path = "MyApp.play";

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_plan_the_resolved_document_for_the_named_application_and_static_target() =>
        _planning.Received(1).Plan(
            Arg.Is<ScreenplayRenderRequest>(_ => _.SourcePath == _document && _.ApplicationName == "MyApp" && _.Target == RenderCommand.DefaultRendererTarget),
            Arg.Any<CancellationToken>());
    [Fact] void should_publish_the_complete_plan_to_the_default_destination() =>
        _publication.Received(1).Publish(
            Arg.Is<ArtifactPublicationRequest>(_ => _.Plan == _artifactPlan && _.Destination == Path.Combine(_folder, RenderCommand.DefaultDestination)),
            Arg.Any<CancellationToken>());
    [Fact] void should_recover_before_planning_and_publish_only_after_planning() => Received.InOrder(() =>
    {
        _publication.Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
        _planning.Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>());
        _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
    });
}
