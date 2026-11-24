// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher;

public class when_writing_control_files : given.an_artifact_publication
{
    [Theory]
    [InlineData("staging/artifact.cs")]
    [InlineData("backup/artifact.cs")]
    [InlineData("journal.json")]
    public void should_create_a_self_ignoring_control_directory(string relativePath)
    {
        var control = ArtifactPublicationStorage.ControlPath(_destination);
        var path = Path.Combine(control, relativePath);

        ArtifactPublicationStorage.WriteControlFile(_destination, path, "control bytes");

        File.ReadAllText(Path.Combine(control, ".gitignore")).ShouldEqual("*\n");
        File.ReadAllText(path).ShouldEqual("control bytes");
        File.Exists(ArtifactPublicationStorage.ManifestPath(_destination)).ShouldBeFalse();
        File.Exists(Path.Combine(_destination, ".gitignore")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("staging/artifact.cs")]
    [InlineData("backup/artifact.cs")]
    [InlineData("journal.json")]
    public void should_not_write_control_data_before_the_ignore_file(string relativePath)
    {
        var control = ArtifactPublicationStorage.ControlPath(_destination);
        Directory.CreateDirectory(Path.Combine(control, ".gitignore"));
        var path = Path.Combine(control, relativePath);

        var error = Catch.Exception(() => ArtifactPublicationStorage.WriteControlFile(_destination, path, "control bytes"));

        error.ShouldNotBeNull();
        Directory.GetFiles(control, "*", SearchOption.AllDirectories).ShouldBeEmpty();
        File.Exists(path).ShouldBeFalse();
    }
}
