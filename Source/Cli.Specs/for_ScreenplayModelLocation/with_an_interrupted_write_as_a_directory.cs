// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_an_interrupted_write_as_a_directory : given.a_project
{
    Exception _error;

    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_project, ".screenplay", "pending.json"));
        Play("Models/application.play");
    }

    void Because() => _error = Catch.Exception(() => ScreenplayModelLocation.Locate(_project));

    [Fact] void should_refuse_a_directory_in_place_of_the_journal() => _error.ShouldBeOfExactType<ScreenplayWorkspaceMetadataConflict>();
}
