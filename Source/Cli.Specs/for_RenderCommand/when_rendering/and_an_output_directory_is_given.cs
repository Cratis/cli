// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_an_output_directory_is_given : given.a_render_command
{
    int _result;

    void Establish()
    {
        _settings.Path = "MyApp.play";
        _settings.Destination = "src/MyApp";
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_recover_the_resolved_destination() =>
        _publication.Received(1).Recover(Path.Combine(_folder, "src", "MyApp"), Arg.Any<CancellationToken>());
    [Fact] void should_publish_into_the_resolved_destination() =>
        _publication.Received(1).Publish(
            Arg.Is<ArtifactPublicationRequest>(_ => _.Destination == Path.Combine(_folder, "src", "MyApp")),
            Arg.Any<CancellationToken>());
}
