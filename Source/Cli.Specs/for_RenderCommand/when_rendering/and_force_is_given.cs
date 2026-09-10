// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_force_is_given : given.a_render_command
{
    int _result;

    void Establish() => _settings.Force = true;

    async Task Because() => _result = await Execute();

    [Fact] void should_succeed() => _result.ShouldEqual(ExitCodes.Success);
    [Fact] void should_limit_force_to_the_managed_publication_request() =>
        _publication.Received(1).Publish(Arg.Is<ArtifactPublicationRequest>(_ => _.Force), Arg.Any<CancellationToken>());
}
