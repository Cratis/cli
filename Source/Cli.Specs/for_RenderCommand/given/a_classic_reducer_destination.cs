// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_RenderCommand.given;

public class a_classic_reducer_destination : a_reducer_render_command
{
    protected const string ReadModel = "Orders.Ordering.Totals.Total";
    protected string _reducerPath = null!;
    protected byte[] _reducerBytes = null!;

    async Task Establish()
    {
        await PublishClassic();
        _reducerPath = ArtifactPublicationStorage.ReadManifest(_destination)!.Artifacts
            .Single(artifact => artifact.Path.EndsWith("/Fold.cs", StringComparison.Ordinal)).Path;
        _reducerBytes = await File.ReadAllBytesAsync(ReducerFile);
    }

    protected string ReducerFile => ArtifactPublicationStorage.ArtifactPath(_destination, _reducerPath);
}
