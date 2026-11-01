// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using NSubstitute.ExceptionExtensions;

namespace Cratis.Cli.for_RenderCommand.given;

public class an_event_id_refusal : a_check_command
{
    void Establish()
    {
        _settings.Check = false;
        _command = new RenderCommand(_planning, _publication);
        _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>())
            .Throws(new UnsafeArtifactPublication("STAGE-EVENT-002: Event type 'Records.Renamed' changes from Original to Renamed. Pin the event's 'id'.", "CLI-RENDER-016"));
    }
}
