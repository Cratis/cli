// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_rerendering_a_modified_managed_file : given.an_artifact_publication
{
    const string UserContent = "user modified";
    Exception _error = null!;

    async Task Establish()
    {
        await Publish();
        await File.WriteAllTextAsync(FirstSourcePath(), UserContent);
    }

    async Task Because() => _error = await Catch.Exception(() => Publish());

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<UnsafeArtifactPublication>();
    [Fact] void should_preserve_the_user_change() => File.ReadAllText(FirstSourcePath()).ShouldEqual(UserContent);
}
