// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_rerendering_over_a_wrapper_without_execute : given.a_wrapper_script_publication
{
    ArtifactPublicationResult _result = null!;

    async Task Establish()
    {
        await Publish();
        RemoveExecute("mvnw");
    }

    async Task Because() => _result = await Publish();

    [Fact] void should_not_rewrite_the_unchanged_wrapper() => _result.Written.ShouldEqual(0);
    [Fact] void should_make_the_existing_wrapper_executable() => HasOwnerExecute("mvnw").ShouldBeTrue();
}
