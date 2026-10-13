// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_workspace_state_above_a_nested_model : given.a_project
{
    void Establish()
    {
        State(string.Empty);
        Play("Models/application.play");
    }

    void Because() => _located = ScreenplayModelLocation.Locate(_project);

    [Fact] void should_keep_serving_the_folder_holding_the_state() => _located.ShouldEqual(_project);
}
