// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_a_dangling_workspace_metadata_link : given.a_project
{
    Exception _error;

    void Establish()
    {
        var missing = Path.Combine(Path.GetTempPath(), $"cratis-missing-{Guid.NewGuid():N}");
        Directory.CreateSymbolicLink(Path.Combine(_project, ".screenplay"), missing);
        Play("Models/application.play");
    }

    void Because() => _error = Catch.Exception(() => ScreenplayModelLocation.Locate(_project));

    [Fact] void should_refuse_the_dangling_link() => _error.ShouldBeOfExactType<ScreenplayWorkspaceMetadataConflict>();
}
