// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_workspace_metadata_as_a_file : given.a_project
{
    Exception _error;

    void Establish()
    {
        File.WriteAllText(Path.Combine(_project, ".screenplay"), string.Empty);
        Play("Models/application.play");
    }

    void Because() => _error = Catch.Exception(() => ScreenplayModelLocation.Locate(_project));

    [Fact] void should_refuse_a_file_in_place_of_the_folder() => _error.ShouldBeOfExactType<ScreenplayWorkspaceMetadataConflict>();
}
