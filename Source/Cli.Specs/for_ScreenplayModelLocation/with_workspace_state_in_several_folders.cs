// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_workspace_state_in_several_folders : given.a_project
{
    readonly List<string> _reports = [];

    void Establish()
    {
        State(string.Empty);
        State("Models");
        Play("Models/application.play");
    }

    void Because() => _located = ScreenplayModelLocation.Locate(_project, _reports.Add);

    [Fact] void should_serve_the_folder_nearest_the_project() => _located.ShouldEqual(_project);
    [Fact] void should_report_the_other_folder_holding_state() => _reports.Single().ShouldContain(PathOf("Models"));
}
