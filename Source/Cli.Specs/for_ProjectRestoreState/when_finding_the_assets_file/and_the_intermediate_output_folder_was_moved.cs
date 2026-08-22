// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ProjectRestoreState.when_finding_the_assets_file;

public class and_the_intermediate_output_folder_was_moved : given.a_project_folder
{
    string _assets;
    string _result;

    void Establish()
    {
        Restore(Path.Combine(_folder, "obj"));
        var intermediate = Path.Combine(_folder, "artifacts", "obj", "MyApp");
        _assets = Restore(intermediate);
        _result = Path.Combine(intermediate, "Debug", "net9.0", "MyApp.dll");
    }

    void Because() => _result = ProjectRestoreState.AssetsFileFor(_project, _result)!;

    [Fact] void should_find_the_assets_file_from_the_intermediate_assembly() => _result.ShouldEqual(_assets);
}
