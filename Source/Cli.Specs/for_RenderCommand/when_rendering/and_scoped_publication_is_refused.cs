// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class and_scoped_publication_is_refused : given.a_render_command
{
    int _result;

    void Establish()
    {
        _settings.Scope = ["Orders.Placing"];
        _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>())
            .Throws(new UnsafeArtifactPublication("Render the whole application once", "CLI-RENDER-013"));
    }

    async Task Because() => _result = await Execute();

    [Fact] void should_return_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
}
