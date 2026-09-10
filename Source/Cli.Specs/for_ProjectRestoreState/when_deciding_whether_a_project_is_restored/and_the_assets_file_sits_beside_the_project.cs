// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProjectRestoreState.when_deciding_whether_a_project_is_restored;

public class and_the_assets_file_sits_beside_the_project : given.a_project_folder
{
    bool _result;

    void Establish() => Restore(Path.Combine(_folder, "obj"));

    void Because() => _result = ProjectRestoreState.IsRestored(_project, Path.Combine(_folder, "obj", "Debug", "net10.0", "MyApp.dll"));

    [Fact] void should_take_the_project_for_restored() => _result.ShouldBeTrue();
}
