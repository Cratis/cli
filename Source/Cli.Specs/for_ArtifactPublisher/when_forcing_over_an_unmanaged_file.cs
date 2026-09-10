// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_forcing_over_an_unmanaged_file : given.an_artifact_publication
{
    const string UserContent = "user owned";
    Exception _error = null!;

    void Establish()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FirstSourcePath())!);
        File.WriteAllText(FirstSourcePath(), UserContent);
    }

    async Task Because() => _error = await Catch.Exception(() => Publish(force: true));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<UnsafeArtifactPublication>();
    [Fact] void should_preserve_the_user_file() => File.ReadAllText(FirstSourcePath()).ShouldEqual(UserContent);
}
