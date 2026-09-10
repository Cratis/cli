// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProjectRestoreState.when_deciding_whether_a_project_is_restored;

public class and_nothing_has_been_restored : given.a_project_folder
{
    bool _result;

    void Because() => _result = ProjectRestoreState.IsRestored(_project, Path.Combine(_folder, "obj", "Debug", "net10.0", "MyApp.dll"));

    [Fact] void should_report_the_project_as_unrestored() => _result.ShouldBeFalse();
}
