// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_workspace_state_and_no_model : given.a_project
{
    void Establish() => State(string.Empty);

    void Because() => _located = ScreenplayModelLocation.Locate(_project);

    [Fact] void should_serve_the_folder_holding_the_state() => _located.ShouldEqual(_project);
    [Fact] void should_not_create_a_folder() => Directory.Exists(PathOf("Screenplay")).ShouldBeFalse();
}
