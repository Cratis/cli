// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_malformed_workspace_metadata : given.a_project
{
    Exception _error;

    void Establish()
    {
        Directory.CreateDirectory(Path.Combine(_project, ".screenplay", "identities.json"));
        Play("Models/application.play");
    }

    void Because() => _error = Catch.Exception(() => ScreenplayModelLocation.Locate(_project));

    [Fact] void should_refuse_to_choose_a_folder() => _error.ShouldBeOfExactType<ScreenplayWorkspaceMetadataConflict>();
    [Fact] void should_name_the_metadata_folder() => _error.Message.ShouldContain(PathOf(".screenplay"));
}
