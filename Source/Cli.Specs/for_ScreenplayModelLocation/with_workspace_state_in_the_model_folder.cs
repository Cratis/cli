// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_workspace_state_in_the_model_folder : given.a_project
{
    void Establish()
    {
        State("Models");
        Play("Models/application.play");
    }

    void Because() => _located = ScreenplayModelLocation.Locate(_project);

    [Fact] void should_serve_the_model_folder() => _located.ShouldEqual(PathOf("Models"));
}
