// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_a_baseline_is_given_with_a_path : given.a_render_command
{
    int _result;

    void Establish() => _settings.Baseline = "release-baseline.json";
    async Task Because() => _result = await Execute();

    [Fact] void should_return_usage_error() => _result.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_not_plan() => _planning.DidNotReceive().Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_not_recover() => await _publication.DidNotReceive().Recover(Arg.Any<string>(), Arg.Any<CancellationToken>());
    [Fact] async Task should_not_publish() => await _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
}
