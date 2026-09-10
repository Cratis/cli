// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_publication_is_unsafe : given.a_render_command
{
    int _result;

    void Establish() =>
        _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ArtifactPublicationResult>(new UnsafeArtifactPublication("unmanaged collision")));

    async Task Because() => _result = await Execute();

    [Fact] void should_report_a_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
}
