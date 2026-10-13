// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_an_interrupted_write_in_another_folder : given.a_project
{
    Exception _error;

    void Establish()
    {
        State(string.Empty);
        State("Models", interrupted: true);
        Play("Models/application.play");
    }

    void Because() => _error = Catch.Exception(() => ScreenplayModelLocation.Locate(_project));

    [Fact] void should_refuse_to_choose_a_folder() => _error.ShouldBeOfExactType<ScreenplayWorkspaceStateConflict>();
    [Fact] void should_name_the_folder_with_the_interrupted_write() => _error.Message.ShouldContain(PathOf("Models"));
}
